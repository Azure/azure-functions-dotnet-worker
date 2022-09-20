// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Azure.Functions.Worker.Sdk.Generators.Enums;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Azure.Functions.Worker.Sdk.Generators
{
    public partial class FunctionMetadataProviderGenerator
    {
        internal sealed class Parser
        {
            private Compilation Compilation => _context.Compilation;
            private readonly GeneratorExecutionContext _context;
            private CancellationToken CancellationToken => _context.CancellationToken;

            public Parser(GeneratorExecutionContext context)
            {
                _context = context;
            }

            /// <summary>
            /// Takes in candidate methods from the user compilation and parses them to return function metadata info as GeneratorFunctionMetadata.
            /// </summary>
            /// <param name="methods">List of candidate methods from the syntax receiver.</param>
            /// <returns></returns>
            public IReadOnlyList<GeneratorFunctionMetadata> GetFunctionMetadataInfo(List<MethodDeclarationSyntax> methods)
            {
                var result = ImmutableArray.CreateBuilder<GeneratorFunctionMetadata>();

                var assemblyName = Compilation.Assembly.Name;
                var scriptFile = Path.Combine(assemblyName + ".dll");

                // Loop through the candidate methods (methods with any attribute associated with them)
                foreach (MethodDeclarationSyntax method in methods)
                {
                    CancellationToken.ThrowIfCancellationRequested();

                    var model = Compilation.GetSemanticModel(method.SyntaxTree);

                    if (!IsValidMethodAzureFunction(model, method, out string functionName))
                    {
                        continue;
                    }

                    var functionClass = (ClassDeclarationSyntax) method.Parent!;
                    var functionClassName = functionClass.Identifier.ValueText;
                    var newFunction = new GeneratorFunctionMetadata
                    {
                        Name = functionName,
                        EntryPoint = assemblyName + "." + functionClassName + "." + method.Identifier.ValueText,
                        FunctionId = Guid.NewGuid().ToString(),
                        Language = "dotnet-isolated",
                        ScriptFile = scriptFile
                    };

                    if(!TryGetBindings(method, model, out IList<IDictionary<string, string>>? bindings, out bool hasHttpTrigger) || bindings is null)
                    {
                        continue;
                    }

                    if(hasHttpTrigger)
                    {
                        newFunction.IsHttpTrigger = true;
                    }

                    newFunction.RawBindings = bindings;

                    result.Add(newFunction);
                }

                return result; 
            }

            /// <summary>
            /// Formats an object into a string value for the source-generated file. This can mean adding quotation marks around the string
            /// representation of the object, or leaving it as is if the object is a string or Enum type.
            /// </summary>
            /// <param name="propValue">The property that needs to be formmated into a string.</param>
            /// <returns></returns>
            private string FormatObject(object propValue, bool isEnum = false)
            {
                if (propValue is null)
                {
                    return "null";
                }

                // catch values that are already strings or Enum parsing
                // we don't need to surround these cases with quotation marks
                if (propValue.ToString().Contains("\"") || isEnum)
                {
                    return propValue.ToString();
                }

                return "\"" + propValue.ToString() + "\"";
            }

            /// <summary>
            /// Format an array into a string.
            /// </summary>
            /// <param name="enumerableValues">An array object to be formatted.</param>
            /// <returns></returns>
            private string FormatArray(IEnumerable enumerableValues)
            {
                string arrAsString;

                arrAsString = "new List<string> { ";

                foreach (var o in enumerableValues)
                {
                    arrAsString += FormatObject(o);
                    arrAsString += ",";
                }

                arrAsString = arrAsString.TrimEnd(',', ' ');
                arrAsString += " }";

                return arrAsString;
            }

            /// <summary>
            /// Checks if a candidate method has a Function attribute on it.
            /// </summary>
            private bool IsValidMethodAzureFunction(SemanticModel model, MethodDeclarationSyntax method, out string functionName)
            {
                functionName = string.Empty;
                var methodSymbol = model.GetDeclaredSymbol(method);

                if (methodSymbol is null)
                {
                    _context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.SymbolNotFound, method.Identifier.GetLocation(), nameof(methodSymbol)));
                    return false;
                }

                foreach (var attr in methodSymbol.GetAttributes())
                {
                    if (attr.AttributeClass != null &&
                       SymbolEqualityComparer.Default.Equals(attr.AttributeClass, Compilation.GetTypeByMetadataName(Constants.FunctionNameType)))
                    {
                        functionName = (string)attr.ConstructorArguments.First().Value!; // If this is a function attribute this won't be null
                        return true;
                    }
                }

                return false;
            }

            private bool TryGetBindings(MethodDeclarationSyntax method, SemanticModel model, out IList<IDictionary<string, string>>? bindings, out bool hasHttpTrigger)
            {
                var result = new List<IDictionary<string, string>>();

                var outputSucceed = TryGetMethodOutputBinding(method, model, out bool hasOutputBinding, out IList<IDictionary<string, string>>? methodOutputBindings);
                var inputSucceed = TryGetParameterInputAndTriggerBindings(method, model, out hasHttpTrigger, out IList<IDictionary<string, string>>? parameterInputAndTriggerBindings);
                var returnSucceed = TryGetReturnTypeBindings(method, model, hasHttpTrigger, hasOutputBinding, out IList<IDictionary<string, string>>? returnTypeBindings);
                
                // if there was an error with any of the binding retrieval methods, return an empty list
                if (!outputSucceed || !inputSucceed || !returnSucceed)
                {
                    bindings = null;
                    return false;
                }

                result.AddRange(methodOutputBindings);
                result.AddRange(parameterInputAndTriggerBindings);
                result.AddRange(returnTypeBindings);
                bindings = result;

                return true;
            }

            /// <summary>
            /// Checks for and returns any OutputBinding attributes associated with the method.
            /// </summary>
            private bool TryGetMethodOutputBinding(MethodDeclarationSyntax method, SemanticModel model, out bool hasOutputBinding, out IList<IDictionary<string, string>>? bindingsList)
            {
                var bindingLocation = method.Identifier.GetLocation();

                var methodSymbol = model.GetDeclaredSymbol(method);
                var attributes = methodSymbol!.GetAttributes(); // methodSymbol is not null here because it's checked in IsMethodAFunction which is called before bindings are collected/created

                AttributeData? outputBinding = null;
                hasOutputBinding = false;

                foreach (var attribute in attributes)
                {
                    if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass?.BaseType?.BaseType, Compilation.GetTypeByMetadataName(Constants.BindingAttributeType)))
                    {
                        // There can only be one output binding associated with a function. If there is more than one, we return a diagnostic error here.
                        if (hasOutputBinding)
                        {
                            _context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.MultipleBindingsGroupedTogether, bindingLocation, method.ToString()));
                            bindingsList = null;
                            return false;
                        }

                        outputBinding = attribute;
                        hasOutputBinding = true;
                    }
                }

                if (outputBinding != null)
                {
                    if (!TryCreateBindingDict(outputBinding, Constants.ReturnBindingName, bindingLocation, out IDictionary<string, string>? bindingDict) || bindingDict is null)
                    {
                        bindingsList = null;
                        return false;
                    }

                    bindingsList = new List<IDictionary<string, string>>();
                    bindingsList.Add(bindingDict);
                    return true;
                }

                // we didn't find any output bindings on the method, but there were no errors
                // so we treat the found bindings as an empty list and return true
                bindingsList = new List<IDictionary<string, string>>();
                return true;
            }

            /// <summary>
            /// Checks for and returns input and trigger bindings found in the parameters of the Azure Function method.
            /// </summary>
            private bool TryGetParameterInputAndTriggerBindings(MethodDeclarationSyntax method, SemanticModel model, out bool hasHttpTrigger, out IList<IDictionary<string, string>>? bindingsList)
            {
                hasHttpTrigger = false;
                bindingsList = new List<IDictionary<string, string>>();

                foreach (ParameterSyntax parameter in method.ParameterList.Parameters)
                {
                    // If there's no attribute, we can assume that this parameter is not a binding
                    if (parameter.AttributeLists.Count == 0)
                    {
                        continue;
                    }

                    if (model.GetDeclaredSymbol(parameter) is not IParameterSymbol parameterSymbol)
                    {
                        _context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.SymbolNotFound, parameter.Identifier.GetLocation(), nameof(parameterSymbol)));
                        bindingsList = null;
                        return false;
                    }

                    // Check to see if any of the attributes associated with this parameter is a BindingAttribute
                    foreach (var attribute in parameterSymbol.GetAttributes())
                    {
                        if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass?.BaseType?.BaseType, Compilation.GetTypeByMetadataName(Constants.BindingAttributeType)))
                        {
                            var validEventHubs = false;
                            var cardinality = Cardinality.Undefined;
                            var dataType = GetDataType(parameterSymbol.Type);

                            // There are two special cases we need to handle: HttpTrigger and EventHubsTrigger.
                            if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, Compilation.GetTypeByMetadataName(Constants.HttpTriggerBindingType)))
                            {
                                hasHttpTrigger = true;
                            }
                            else if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, Compilation.GetTypeByMetadataName(Constants.EventHubsTriggerType)))
                            {
                                // there are special rules for EventHubsTriggers that we will have to validate
                                validEventHubs = IsEventHubsTriggerValid(parameterSymbol, parameter.Type, model, attribute, out dataType, out cardinality);
                                if (!validEventHubs)
                                {
                                    _context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.InvalidEventHubsTrigger, parameter.Identifier.GetLocation(), nameof(parameterSymbol)));
                                    bindingsList = null;
                                    return false;
                                }
                            }

                            string bindingName = parameter.Identifier.ValueText;

                            if (!TryCreateBindingDict(attribute, bindingName, parameter.Identifier.GetLocation(), out IDictionary<string, string>? bindingDict) || bindingDict is null)
                            {
                                bindingsList = null;
                                return false;
                            }
                            
                            if (dataType is not DataType.Undefined)
                            {
                                bindingDict.Add("dataType", FormatObject(Enum.GetName(typeof(DataType), dataType)));
                            }

                            // special handling for EventHubsTrigger - we need to define a property called "Cardinality"
                            if(validEventHubs)
                            {
                                if(!bindingDict.Keys.Contains("Cardinality"))
                                {
                                    bindingDict["Cardinality"] = cardinality is Cardinality.Many ? FormatObject("Many") : FormatObject("One");
                                }
                            }

                            bindingsList.Add(bindingDict);
                        }
                    }
                }

                return true;
            }

            /// <summary>
            /// Checks for and returns any bindings found in the Return Type of the method
            /// </summary>
            private bool TryGetReturnTypeBindings(MethodDeclarationSyntax method, SemanticModel model, bool hasHttpTrigger, bool hasOutputBinding, out IList<IDictionary<string, string>>? bindingsList)
            {
                TypeSyntax returnTypeSyntax = method.ReturnType;
                ITypeSymbol? returnTypeSymbol = model.GetSymbolInfo(returnTypeSyntax).Symbol as ITypeSymbol;
                bindingsList = new List<IDictionary<string, string>>();

                if (returnTypeSymbol is null)
                {
                    _context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.SymbolNotFound, returnTypeSyntax.GetLocation(), nameof(returnTypeSymbol)));
                    bindingsList = null;
                    return false;
                }

                if (returnTypeSymbol != null &&
                    !SymbolEqualityComparer.Default.Equals(returnTypeSymbol, Compilation.GetTypeByMetadataName(Constants.VoidType)) &&
                    !SymbolEqualityComparer.Default.Equals(returnTypeSymbol, Compilation.GetTypeByMetadataName(Constants.TaskType)))
                {
                    // If there is a Task<T> return type, inspect T, the inner type.
                    if (SymbolEqualityComparer.Default.Equals(returnTypeSymbol, Compilation.GetTypeByMetadataName(Constants.TaskGenericType))) 
                    {
                        GenericNameSyntax genericSyntax = (GenericNameSyntax)returnTypeSyntax;
                        var innerTypeSyntax = genericSyntax.TypeArgumentList.Arguments.First(); // Generic task should only have one type argument
                        returnTypeSymbol = model.GetSymbolInfo(innerTypeSyntax).Symbol as ITypeSymbol;

                        if (returnTypeSymbol is null)
                        {
                            _context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.SymbolNotFound, genericSyntax.Identifier.GetLocation(), nameof(returnTypeSymbol)));
                            bindingsList = null;
                            return false;
                        }
                    }

                    if (SymbolEqualityComparer.Default.Equals(returnTypeSymbol, Compilation.GetTypeByMetadataName(Constants.HttpResponseType))) // If return type is HttpResponseData
                    {
                        bindingsList.Add(GetHttpReturnBinding(Constants.ReturnBindingName));
                        hasOutputBinding = true;
                    }
                    else
                    {
                        var members = returnTypeSymbol.GetMembers();
                        var foundHttpOutput = false;

                        foreach (var m in members)
                        {
                            // Check if this attribute is an HttpResponseData type attribute
                            if (m is IPropertySymbol property && SymbolEqualityComparer.Default.Equals(property.Type, Compilation.GetTypeByMetadataName(Constants.HttpResponseType)))
                            {
                                if (foundHttpOutput)
                                {
                                    _context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.MultipleBindingsGroupedTogether, m.Locations.FirstOrDefault(), new object[] { nameof(m), nameof(returnTypeSymbol) }));
                                    bindingsList = null;
                                    return false;
                                }

                                foundHttpOutput = true;
                                bindingsList.Add(GetHttpReturnBinding(m.Name));
                            }
                            else if (m.GetAttributes().Length > 0) // check if this property has any attributes
                            {
                                var foundPropertyOutputAttr = false;

                                foreach (var attr in m.GetAttributes()) // now loop through and check if any of the attributes are Binding attributes
                                {
                                    if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass?.BaseType?.BaseType, Compilation.GetTypeByMetadataName(Constants.BindingAttributeType)))
                                    {
                                        // validate that there's only one binding attribute per proeprty
                                        if (foundPropertyOutputAttr)
                                        {
                                            _context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.MultipleBindingsGroupedTogether, m.Locations.FirstOrDefault(), new object[] { nameof(m), nameof(returnTypeSymbol) }));
                                            bindingsList = null;
                                            return false;
                                        }

                                        IPropertySymbol? propertySymbol = m as IPropertySymbol;

                                        if (propertySymbol is null)
                                        {
                                            throw new InvalidOperationException($"The property '{nameof(propertySymbol)}' is invalid.");
                                        }

                                        var location = m.Locations.FirstOrDefault();
                                        if (location is null)
                                        {
                                            location = Location.None;
                                        }

                                        if(!TryCreateBindingDict(attr, m.Name, location, out IDictionary<string, string>? bindingDict) || bindingDict is null)
                                        {
                                            bindingsList = null;
                                            return false;
                                        }

                                        bindingsList.Add(bindingDict);

                                        hasOutputBinding = true;
                                        foundPropertyOutputAttr = true;
                                    }
                                }

                            }
                        }

                        if (hasHttpTrigger && !foundHttpOutput)
                        {
                            if (!hasOutputBinding)
                            {
                                bindingsList.Add(GetHttpReturnBinding(Constants.ReturnBindingName));
                            }
                            else
                            {
                                bindingsList.Add(GetHttpReturnBinding(Constants.HttpResponseBindingName));
                            }
                        }
                    }
                }

                return true;
            }

            private IDictionary<string, string> GetHttpReturnBinding(string returnBindingName)
            {
                var httpBinding = new Dictionary<string, string>();

                httpBinding.Add("name", FormatObject(returnBindingName));
                httpBinding.Add("type", FormatObject("http"));
                httpBinding.Add("direction", FormatObject("Out"));

                return httpBinding;
            }

            private bool TryCreateBindingDict(AttributeData bindingAttrData, string bindingName, Location bindingLocation, out IDictionary<string, string>? bindings)
            {
                IMethodSymbol? attribMethodSymbol = bindingAttrData.AttributeConstructor;

                // Check if the attribute constructor has any parameters
                if (attribMethodSymbol is null || attribMethodSymbol?.Parameters is null)
                {
                    _context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.SymbolNotFound, bindingLocation, nameof(attribMethodSymbol)));
                    bindings = null;
                    return false;
                }

                // Get binding info as a dictionary with keys as the property name and value as the property value
                if(!TryGetAttributeProperties(attribMethodSymbol, bindingAttrData, bindingLocation, out IDictionary<string, object>? attributeProperties))
                {
                    bindings = null;
                    return false;
                }

                // Grab some required binding info properties
                string attributeName = bindingAttrData.AttributeClass!.Name;

                // properly format binding types by removing "Attribute" and "Input" descriptors
                string bindingType = attributeName.TrimStringsFromEnd(new string[] { "Attribute", "Input", "Output" });

                // Set binding direction
                string bindingDirection = SymbolEqualityComparer.Default.Equals(bindingAttrData.AttributeClass?.BaseType, Compilation.GetTypeByMetadataName(Constants.OutputBindingAttributeType)) ? "Out" : "In";

                bindings = new Dictionary<string, string>();
                bindings.Add("name", FormatObject(bindingName));
                bindings.Add("type", FormatObject(bindingType));
                bindings.Add("direction", FormatObject(bindingDirection));

                // Add additional bindingInfo to the anonymous type because some functions have more properties than others
                foreach (var prop in attributeProperties!) // attributeProperties won't be null here b/c we would've exited this method earlier if it was during TryGetAttributeProperties check
                {
                    var propertyName = prop.Key;

                    if (prop.Value.GetType().IsArray)
                    {
                        string arr = FormatArray((IEnumerable)prop.Value);
                        bindings[propertyName] = arr;
                    }
                    else
                    {
                        var isEnum = false;
                        
                        if(string.Equals(prop.Key.ToString(), "authLevel", StringComparison.OrdinalIgnoreCase)) // prop keys come from Azure Functions defined attributes so we can check directly for authLevel
                        {
                            isEnum = true;
                        }

                        var propertyValue = FormatObject(prop.Value, isEnum);
                        bindings[propertyName] = propertyValue;
                    }
                }

                return true;
            }

            private bool TryGetAttributeProperties(IMethodSymbol attribMethodSymbol, AttributeData attributeData, Location attribLocation, out IDictionary<string, object>? attrProperties)
            {
                attrProperties = new Dictionary<string, object>();

                if (attributeData.ConstructorArguments.Any())
                {
                    if(!TryLoadConstructorArguments(attribMethodSymbol, attributeData, attrProperties, attribLocation))
                    {
                        attrProperties = null;
                        return false;
                    }
                }

                foreach (var namedArgument in attributeData.NamedArguments)
                {
                    if (namedArgument.Value.Value != null)
                    {
                        if (String.Equals(namedArgument.Key, Constants.IsBatchedKey) && !attrProperties.Keys.Contains("Cardinality"))
                        {
                            var argValue = (bool)namedArgument.Value.Value; // isBatched only takes in booleans and the generator will parse it as a bool so we can type cast this to use in the next line

                            attrProperties["Cardinality"] = argValue ? "Many" : "One";
                        }
                        else
                        {
                            attrProperties[namedArgument.Key] = namedArgument.Value.Value;
                        }
                    }
                }

                return true;
            }

            private bool TryLoadConstructorArguments(IMethodSymbol attribMethodSymbol, AttributeData attributeData, IDictionary<string, object> dict, Location attribLocation)
            {
                if (attribMethodSymbol.Parameters.Length != attributeData.ConstructorArguments.Length)
                {
                    _context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.ArgumentCountMismatch, attribLocation, new object[] { nameof(attribMethodSymbol), nameof(attributeData) }));
                    return false;
                }

                // It's fair to assume than constructor arguments appear before named arguments, and
                // that the constructor names would match the property names
                for (int i = 0; i < attributeData.ConstructorArguments.Length; i++)
                {
                    var argumentName = attribMethodSymbol.Parameters[i].Name;
                    var arg = attributeData.ConstructorArguments[i];

                    switch (arg.Kind)
                    {
                        case TypedConstantKind.Error:
                            break;
                        case TypedConstantKind.Primitive:
                            dict[argumentName] = arg.Value;
                            break;
                        case TypedConstantKind.Enum:
                            dict[argumentName] = $"(AuthorizationLevel){arg.Value}"; // the only enum type we have in function metadata is authlevel
                            break;
                        case TypedConstantKind.Type:
                            break;
                        case TypedConstantKind.Array:
                            var arrayValues = arg.Values.Select(a => a.Value!.ToString()).ToArray();
                            dict[argumentName] = arrayValues;
                            break;
                        default:
                            break;
                    }
                }
                return true;
            }

            /// <summary>
            /// This method verifies that an EventHubsTrigger matches our expectations on cardinality (isBatched property). If isBatched is set to true, the parameter with the
            /// attriute must be an enumerabletype.
            /// </summary>
            private bool IsEventHubsTriggerValid(IParameterSymbol parameterSymbol, TypeSyntax? parameterTypeSyntax, SemanticModel model, AttributeData attribute, out DataType dataType, out Cardinality cardinality)
            {
                dataType = DataType.Undefined;
                cardinality = Cardinality.Undefined;

                // Check if IsBatched is false (by default it is true and does not appear in the attribute constructor)
                foreach (var arg in attribute.NamedArguments)
                {
                    if (String.Equals(arg.Key, Constants.IsBatchedKey) &&
                        arg.Value.Value != null)
                    {
                        var isBatched = (bool)arg.Value.Value; // isBatched takes in booleans so we can just type cast it here to use

                        if (!isBatched)
                        {
                            dataType = GetDataType(parameterSymbol.Type);
                            cardinality = Cardinality.One;
                            return true;
                        }
                    }
                }

                // we check if the param is an array type
                // we exclude byte arrays (byte[]) b/c we handle that as cardinality one (we handle this simliar to how a char[] is basically a string)
                if (parameterSymbol.Type is IArrayTypeSymbol && !SymbolEqualityComparer.Default.Equals(parameterSymbol.Type, Compilation.GetTypeByMetadataName(Constants.ByteArrayType)))
                {
                    dataType = GetDataType(parameterSymbol.Type);
                    cardinality = Cardinality.Many;
                    return true;
                }

                var isGenericEnumerable = parameterSymbol.Type.IsOrImplementsOrDerivesFrom(Compilation.GetTypeByMetadataName(Constants.IEnumerableGenericType));
                var isEnumerable = parameterSymbol.Type.IsOrImplementsOrDerivesFrom(Compilation.GetTypeByMetadataName(Constants.IEnumerableType));

                // Check if mapping type
                if (parameterSymbol.Type.IsOrImplementsOrDerivesFrom(Compilation.GetTypeByMetadataName(Constants.IEnumerableOfKeyValuePair))
                    || parameterSymbol.Type.IsOrImplementsOrDerivesFrom(Compilation.GetTypeByMetadataName(Constants.LookupGenericType))
                    || parameterSymbol.Type.IsOrImplementsOrDerivesFrom(Compilation.GetTypeByMetadataName(Constants.DictionaryGenericType)))
                {
                    return false;
                }

                if (!IsStringType(parameterSymbol.Type) && (isGenericEnumerable || isEnumerable))
                {
                    cardinality = Cardinality.Many;
                    if (IsStringType(parameterSymbol.Type))
                    {
                        dataType = DataType.String;
                    }
                    else if (IsBinaryType(parameterSymbol.Type))
                    {
                        dataType = DataType.Binary;
                    }
                    else if (isGenericEnumerable)
                    {
                        if(parameterTypeSyntax is null)
                        {
                            return false;
                        }

                        dataType = ResolveIEnumerableOfT(parameterSymbol, parameterTypeSyntax, model, out bool hasError);

                        if(hasError)
                        {
                            return false;
                        }

                        return true;
                    }
                    return true;
                }
                
                // trigger input type doesn't match any of the valid cases so return false
                return false;
            }

            /// <summary>
            /// Find the underlying data type of an IEnumerableOfT (String, Binary, Undefined)
            /// ex. IEnumerable<byte[]> would return DataType.Binary
            /// </summary>
            private DataType ResolveIEnumerableOfT(IParameterSymbol parameterSymbol, TypeSyntax parameterSyntax, SemanticModel model, out bool hasError)
            {
                var result = DataType.Undefined;
                var currentSyntax = parameterSyntax;
                hasError = false;

                var currSymbol = parameterSymbol.Type;
                INamedTypeSymbol? finalSymbol = null;

                while(currSymbol != null)
                {
                    INamedTypeSymbol? genericInterfaceSymbol = null;

                    if (currSymbol.IsOrDerivedFrom(Compilation.GetTypeByMetadataName(Constants.IEnumerableGenericType)) && currSymbol is INamedTypeSymbol currNamedSymbol)
                    {
                        finalSymbol = currNamedSymbol;
                        break;
                    }

                    genericInterfaceSymbol = currSymbol.Interfaces.Where(i => i.IsOrDerivedFrom(Compilation.GetTypeByMetadataName(Constants.IEnumerableGenericType))).FirstOrDefault();
                    if(genericInterfaceSymbol != null)
                    {
                        finalSymbol = genericInterfaceSymbol;
                        break;
                    }

                    currSymbol = currSymbol.BaseType;
                }

                if (finalSymbol is null)
                {
                    hasError = true;
                    return result;
                }

                var argument = finalSymbol.TypeArguments.FirstOrDefault(); // we've already checked and discarded mapping types by this point - should be a single argument

                if(argument is null)
                {
                    hasError = true;
                    return result;
                }

                return GetDataType(argument);
            }

            private DataType GetDataType(ITypeSymbol symbol)
            {
                if (IsStringType(symbol))
                {
                    return DataType.String;
                }
                // Is binary parameter type
                else if (IsBinaryType(symbol))
                {
                    return DataType.Binary;
                }

                return DataType.Undefined;
            }

            private bool IsStringType(ITypeSymbol symbol)
            {
                return SymbolEqualityComparer.Default.Equals(symbol, Compilation.GetTypeByMetadataName(Constants.StringType))
                    || (symbol is IArrayTypeSymbol arraySymbol && SymbolEqualityComparer.Default.Equals(arraySymbol.ElementType, Compilation.GetTypeByMetadataName(Constants.StringType)));
            }

            private bool IsBinaryType(ITypeSymbol symbol)
            {
                var isByteArray = SymbolEqualityComparer.Default.Equals(symbol, Compilation.GetTypeByMetadataName(Constants.ByteArrayType))
                    || (symbol is IArrayTypeSymbol arraySymbol && SymbolEqualityComparer.Default.Equals(arraySymbol.ElementType, Compilation.GetTypeByMetadataName(Constants.ByteStructType)));
                var isReadOnlyMemoryOfBytes = SymbolEqualityComparer.Default.Equals(symbol, Compilation.GetTypeByMetadataName(Constants.ReadOnlyMemoryOfBytes));
                var isArrayOfByteArrays = symbol is IArrayTypeSymbol outerArray && 
                    outerArray.ElementType is IArrayTypeSymbol innerArray && SymbolEqualityComparer.Default.Equals(innerArray.ElementType, Compilation.GetTypeByMetadataName(Constants.ByteStructType));


                return isByteArray || isReadOnlyMemoryOfBytes || isArrayOfByteArrays;
            }
        }
    }
}
