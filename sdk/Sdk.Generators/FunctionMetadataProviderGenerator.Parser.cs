// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.Azure.Functions.Worker.Sdk.Generators.Enums;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Azure.Functions.Worker.Sdk.Generators
{
    public partial class FunctionMetadataProviderGenerator
    {
        internal sealed class Parser
        {
            private readonly Compilation _compilation;
            private readonly GeneratorExecutionContext _context;

            public Parser(GeneratorExecutionContext context)
            {
                _compilation = context.Compilation;
                _context = context;
            }

            public IReadOnlyList<GeneratorFunctionMetadata> GetFunctionMetadataInfo(List<MethodDeclarationSyntax> methods)
            {
                var result = new List<GeneratorFunctionMetadata>();

                var assemblyName = _compilation.Assembly.Name;
                var scriptFile = Path.Combine(assemblyName + ".dll");

                // Loop through the candidate methods (methods with any attribute associated with them)
                foreach (MethodDeclarationSyntax method in methods)
                {
                    var model = _compilation.GetSemanticModel(method.SyntaxTree);

                    if (!IsMethodAzureFunction(model, method, out string functionName, out bool hasError))
                    {
                        continue;
                    }

                    if(hasError)
                    {
                        return Array.Empty<GeneratorFunctionMetadata>();
                    }

                    var functionClass = (ClassDeclarationSyntax)method.Parent!;
                    var functionClassName = functionClass.Identifier.ValueText;
                    var newFunction = new GeneratorFunctionMetadata
                    {
                        Name = functionName,
                        EntryPoint = assemblyName + "." + functionClassName + "." + method.Identifier.ValueText,
                        FunctionId = Guid.NewGuid().ToString(),
                        Language = "dotnet-isolated",
                        ScriptFile = scriptFile
                    };

                    // collect Bindings
                    var bindings = GetBindings(method, model, functionName, out bool hasBindingError, out bool hasHttpTrigger);

                    if(hasBindingError)
                    {
                        return Array.Empty<GeneratorFunctionMetadata>();
                    }

                    if(hasHttpTrigger)
                    {
                        newFunction.IsHttpTrigger = true;
                    }

                    newFunction.RawBindings = bindings;

                    result.Add(newFunction);
                }

                return result.ToImmutableArray(); 
            }

            /// <summary>
            /// Formats an object into a string value for the source-generated file. This can mean adding quotation marks around the string
            /// representation of the object, or leaving it as is if the object is a string or Enum type.
            /// </summary>
            /// <param name="propValue">The property that needs to be formmated into a string.</param>
            /// <returns></returns>
            public string FormatObject(object propValue)
            {
                if (propValue != null)
                {
                    // catch values that are already strings or Enum parsing
                    // we don't need to surround these cases with quotation marks
                    if (propValue.ToString().Contains("\"") || propValue.ToString().Contains("AuthorizationLevel"))
                    {
                        return propValue.ToString();
                    }

                    return "\"" + propValue.ToString() + "\"";
                }
                else
                {
                    return "null";
                }
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


            private bool IsMethodAzureFunction(SemanticModel model, MethodDeclarationSyntax method, out string functionName, out bool hasError)
            {
                functionName = String.Empty;
                hasError = false;
                var methodSymbol = model.GetDeclaredSymbol(method);

                if (methodSymbol is null)
                {
                    _context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.SymbolNotFound, method.Identifier.GetLocation(), nameof(methodSymbol)));
                    hasError = true;
                    return false;
                }

                foreach (var attr in methodSymbol.GetAttributes())
                {
                    if (attr.AttributeClass != null &&
                       String.Equals(attr.AttributeClass.GetFullName(), Constants.FunctionNameType))
                    {
                        functionName = (string)attr.ConstructorArguments.First().Value!; // If this is a function attribute this won't be null
                        return true;
                    }
                }

                return false;
            }


            private IList<IDictionary<string, string>> GetBindings(MethodDeclarationSyntax method, SemanticModel model, string functionName, out bool hasError, out bool hasHttpTrigger)
            {
                var result = new List<IDictionary<string, string>>();
                hasError = false;

                var methodOutputBindings = GetMethodOutputBinding(method, model, out bool hasOutputBinding, out bool hasOutputError);
                result.AddRange(methodOutputBindings);

                var parameterInputAndTriggerBindings = GetParameterInputAndTriggerBindings(method, model, out hasHttpTrigger, out bool hasInputError);
                result.AddRange(parameterInputAndTriggerBindings);


                var returnTypeBindings = GetReturnTypeBindings(method, model, hasHttpTrigger, hasOutputBinding, out bool hasReturnError);
                result.AddRange(returnTypeBindings);

                if (hasOutputError || hasInputError || hasReturnError)
                {
                    hasError = true;
                }

                return result;
            }

            private IList<IDictionary<string, string>> GetMethodOutputBinding(MethodDeclarationSyntax method, SemanticModel model, out bool hasOutputBinding, out bool hasError)
            {
                var bindingLocation = method.Identifier.GetLocation();

                var methodSymbol = model.GetDeclaredSymbol(method);
                var attributes = methodSymbol!.GetAttributes(); // methodSymbol is not null here because it's checked in IsMethodAFunction which is called before bindings are collected/created
                hasError = false;

                AttributeData? outputBinding = null;
                hasOutputBinding = false;

                foreach (var attribute in attributes)
                {
                    if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass?.BaseType?.BaseType, _compilation.GetTypeByMetadataName(Constants.BindingAttributeType)))
                    {
                        if (hasOutputBinding)
                        {
                            _context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.MultipleBindingsOnAttribute, bindingLocation, method.ToString()));
                            hasError = true;
                            return new List<IDictionary<string, string>>();
                        }

                        outputBinding = attribute;
                        hasOutputBinding = true;
                    }
                }

                if(outputBinding != null)
                {
                    var bindings = CreateBindingDict(outputBinding, Constants.ReturnBindingName, bindingLocation, out bool hasCreationError);

                    if(hasCreationError)
                    {
                        hasError = true;
                        return new List<IDictionary<string, string>>();
                    }

                    return new List<IDictionary<string, string>> { bindings };
                }

                return new List<IDictionary<string, string>>();
            }

            private IList<IDictionary<string, string>> GetParameterInputAndTriggerBindings(MethodDeclarationSyntax method, SemanticModel model, out bool hasHttpTrigger, out bool hasError)
            {
                hasError = false;
                hasHttpTrigger = false;
                var bindings = new List<IDictionary<string, string>>();

                foreach (ParameterSyntax parameter in method.ParameterList.Parameters)
                {
                    // If there's no attribute, we can assume that this parameter is not a binding
                    if (parameter.AttributeLists.Count == 0)
                    {
                        continue;
                    }

                    IParameterSymbol? parameterSymbol = model.GetDeclaredSymbol(parameter) as IParameterSymbol;

                    if (parameterSymbol is null)
                    {
                        _context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.SymbolNotFound, parameter.Identifier.GetLocation(), nameof(parameterSymbol)));
                        hasError = true;
                        return new List<IDictionary<string, string>>();
                    }

                    // Check to see if any of the attributes associated with this parameter is a BindingAttribute
                    foreach (var attribute in parameterSymbol.GetAttributes())
                    {
                        if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass?.BaseType?.BaseType, _compilation.GetTypeByMetadataName(Constants.BindingAttributeType)))
                        {
                            var validEventHubs = false;
                            var cardinality = Cardinality.Undefined;
                            var dataType = GetDataType(parameterSymbol.Type);

                            if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, _compilation.GetTypeByMetadataName(Constants.HttpTriggerBindingType)))
                            {
                                hasHttpTrigger = true;
                            }
                            else if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, _compilation.GetTypeByMetadataName(Constants.EventHubsTriggerType)))
                            {
                                validEventHubs = IsEventHubsTriggerValid(parameterSymbol, parameter.Type, model, attribute, out dataType, out cardinality);
                                if (!validEventHubs) // we need the parameterSymbol to validate the EventHubs trigger, so we'll validate it at this step
                                {
                                    _context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.InvalidEventHubsTrigger, parameter.Identifier.GetLocation(), nameof(parameterSymbol)));
                                    hasError = true;
                                    return new List<IDictionary<string, string>>();
                                }
                            }

                            string bindingName = parameter.Identifier.ValueText;

                            var binding = CreateBindingDict(attribute, bindingName, parameter.Identifier.GetLocation(), out bool creationError);
                            
                            if(dataType is not DataType.Undefined)
                            {
                                binding.Add("dataType", FormatObject(Enum.GetName(typeof(DataType), dataType)));
                            }

                            if (creationError)
                            {
                                hasError = true;
                                return new List<IDictionary<string, string>>();
                            }

                            if(validEventHubs)
                            {
                                if(!binding.Keys.Contains("Cardinality"))
                                {
                                    if (cardinality is Cardinality.Many)
                                    {
                                        binding["Cardinality"] = FormatObject("Many");
                                    }
                                    else if (cardinality is Cardinality.One)
                                    {
                                        binding["Cardinality"] = FormatObject("One");
                                    }
                                }
                            }

                            bindings.Add(binding);
                        }
                    }
                }

                return bindings;
            }

            private IList<IDictionary<string, string>> GetReturnTypeBindings(MethodDeclarationSyntax method, SemanticModel model, bool hasHttpTrigger, bool hasOutputBinding, out bool hasError)
            {
                hasError = false;
                TypeSyntax returnTypeSyntax = method.ReturnType;
                ITypeSymbol? returnTypeSymbol = model.GetSymbolInfo(returnTypeSyntax).Symbol as ITypeSymbol;
                var result = new List<IDictionary<string, string>>();

                if (returnTypeSymbol is null)
                {
                    _context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.SymbolNotFound, returnTypeSyntax.GetLocation(), nameof(returnTypeSymbol)));
                    hasError = true;
                    return new List<IDictionary<string, string>>();
                }

                if (returnTypeSymbol != null &&
                    !SymbolEqualityComparer.Default.Equals(returnTypeSymbol, _compilation.GetTypeByMetadataName(Constants.VoidType)) &&
                    !SymbolEqualityComparer.Default.Equals(returnTypeSymbol, _compilation.GetTypeByMetadataName(Constants.TaskType)))
                {
                    if (SymbolEqualityComparer.Default.Equals(returnTypeSymbol, _compilation.GetTypeByMetadataName(Constants.TaskGenericType))) // If there is a Task<T> return type, inspect T, the inner type.
                    {
                        GenericNameSyntax genericSyntax = (GenericNameSyntax)returnTypeSyntax;
                        var innerTypeSyntax = genericSyntax.TypeArgumentList.Arguments.First(); // Generic task should only have one type argument
                        returnTypeSymbol = model.GetSymbolInfo(innerTypeSyntax).Symbol as ITypeSymbol;

                        if (returnTypeSymbol is null)
                        {
                            _context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.SymbolNotFound, genericSyntax.Identifier.GetLocation(), nameof(returnTypeSymbol)));
                            hasError = true;
                            return new List<IDictionary<string, string>>();
                        }
                    }

                    if (SymbolEqualityComparer.Default.Equals(returnTypeSymbol, _compilation.GetTypeByMetadataName(Constants.HttpResponseType))) // If return type is HttpResponseData
                    {
                        result.Add(GetHttpReturnBinding(Constants.ReturnBindingName));
                        hasOutputBinding = true;
                    }
                    else
                    {
                        // Check all the members(properties) of this return type class to see if any of them have a binding attribute associated
                        var members = returnTypeSymbol.GetMembers();
                        var foundHttpOutput = false;

                        foreach (var m in members)
                        {
                            // Check if this attribute is an HttpResponseData type attribute
                            if (m is IPropertySymbol property && SymbolEqualityComparer.Default.Equals(property.Type, _compilation.GetTypeByMetadataName(Constants.HttpResponseType)))
                            {
                                if (foundHttpOutput)
                                {
                                    _context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.MultipleBindingsOnProperty, m.Locations.FirstOrDefault(), new object[] { nameof(m), nameof(returnTypeSymbol) }));
                                    return new List<IDictionary<string, string>>();
                                }

                                foundHttpOutput = true;
                                result.Add(GetHttpReturnBinding(m.Name));
                            }
                            else if (m.GetAttributes().Length > 0)
                            {
                                var foundPropertyOutputAttr = false;

                                foreach (var attr in m.GetAttributes())
                                {
                                    if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass?.BaseType?.BaseType, _compilation.GetTypeByMetadataName(Constants.BindingAttributeType)))
                                    {
                                        if (foundPropertyOutputAttr)
                                        {
                                            _context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.MultipleBindingsOnProperty, m.Locations.FirstOrDefault(), new object[] { nameof(m), nameof(returnTypeSymbol) }));
                                            return new List<IDictionary<string, string>>();
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

                                        var binding = CreateBindingDict(attr, m.Name, location, out bool creationError);

                                        if (creationError)
                                        {
                                            hasError = true;
                                            return new List<IDictionary<string, string>>();
                                        }

                                        result.Add(binding);

                                        hasOutputBinding = true;
                                        foundPropertyOutputAttr = true;
                                    }
                                }

                            }
                        }

                        // No output bindings found in the return type.
                        if (hasHttpTrigger && !foundHttpOutput)
                        {
                            if (!hasOutputBinding)
                            {
                                result.Add(GetHttpReturnBinding(Constants.ReturnBindingName));
                            }
                            else
                            {
                                result.Add(GetHttpReturnBinding(Constants.HttpResponseBindingName));
                            }
                        }
                    }
                }

                return result;
            }

            private IDictionary<string, string> GetHttpReturnBinding(string returnBindingName)
            {
                var httpBinding = new Dictionary<string, string>();

                httpBinding.Add("name", FormatObject(returnBindingName));
                httpBinding.Add("type", FormatObject("http"));
                httpBinding.Add("direction", FormatObject("Out"));

                return httpBinding;
            }

            private IDictionary<string, string> CreateBindingDict(AttributeData bindingAttrData, string bindingName, Location bindingLocation, out bool hasError)
            {
                hasError = false;
                IMethodSymbol? attribMethodSymbol = bindingAttrData.AttributeConstructor;

                // Check if the attribute constructor has any parameters
                if (attribMethodSymbol is null || attribMethodSymbol?.Parameters is null)
                {
                    _context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.SymbolNotFound, bindingLocation, nameof(attribMethodSymbol)));
                    hasError = true;
                    return new Dictionary<string, string>();
                }

                // Get binding info as a dictionary with keys as the property name and value as the property value
                IDictionary<string, object> attributeProperties = GetAttributeProperties(attribMethodSymbol, bindingAttrData, bindingLocation, out hasError);

                if(hasError)
                {
                    return new Dictionary<string, string>();
                }

                // Grab some required binding info properties
                string attributeName = bindingAttrData.AttributeClass!.Name;

                // properly format binding types by removing "Attribute" and "Input" descriptors
                string bindingType = attributeName.TrimStringsFromEnd(new string[] { "Attribute", "Input", "Output" });

                // Set binding direction
                string bindingDirection = SymbolEqualityComparer.Default.Equals(bindingAttrData.AttributeClass?.BaseType, _compilation.GetTypeByMetadataName(Constants.OutputBindingAttributeType)) ? "Out" : "In";

                var bindingDict = new Dictionary<string, string>();
                bindingDict.Add("name", FormatObject(bindingName));
                bindingDict.Add("type", FormatObject(bindingType));
                bindingDict.Add("direction", FormatObject(bindingDirection));

                // Add additional bindingInfo to the anonymous type because some functions have more properties than others
                foreach (var prop in attributeProperties)
                {
                    var propertyName = prop.Key;

                    if (prop.Value.GetType().IsArray)
                    {
                        string arr = FormatArray((IEnumerable)prop.Value);
                        bindingDict[propertyName] = arr;
                    }
                    else
                    {
                        var propertyValue = FormatObject(prop.Value);
                        bindingDict[propertyName] = propertyValue;
                    }
                }

                return bindingDict;
            }

            private IDictionary<string, object> GetAttributeProperties(IMethodSymbol attribMethodSymbol, AttributeData attributeData, Location attribLocation, out bool hasError)
            {
                hasError = false;
                Dictionary<string, object> argumentData = new();
                if (attributeData.ConstructorArguments.Any())
                {
                    if(!LoadConstructorArguments(attribMethodSymbol, attributeData, argumentData, attribLocation))
                    {
                        hasError = true;
                        return new Dictionary<string, object>();
                    }
                }

                foreach (var namedArgument in attributeData.NamedArguments)
                {
                    if (namedArgument.Value.Value != null)
                    {
                        if (String.Equals(namedArgument.Key, Constants.IsBatchedKey))
                        {
                            var argValue = (bool)namedArgument.Value.Value; // isBatched only takes in booleans and the generator will parse it as a bool so we can type cast this to use in the next line

                            if (argValue && !argumentData.Keys.Contains("Cardinality"))
                            {
                                argumentData["Cardinality"] = "Many";
                            }
                            else
                            {
                                argumentData["Cardinality"] = "One";
                            }
                        }
                        else
                        {
                            argumentData[namedArgument.Key] = namedArgument.Value.Value;
                        }
                    }
                }

                return argumentData;
            }

            private bool LoadConstructorArguments(IMethodSymbol attribMethodSymbol, AttributeData attributeData, IDictionary<string, object> dict, Location attribLocation)
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

                bool isArray = parameterSymbol.Type is IArrayTypeSymbol && !SymbolEqualityComparer.Default.Equals(parameterSymbol.Type, _compilation.GetTypeByMetadataName(Constants.ByteArrayType));

                if (isArray && !SymbolEqualityComparer.Default.Equals(parameterSymbol, _compilation.GetTypeByMetadataName(Constants.TaskType)))
                {
                    dataType = GetDataType(parameterSymbol.Type);
                    cardinality = Cardinality.Many;
                    return true;
                }

                // we check if it's a generic of IEnumerable, IList, or HashSet
                // Roslyn symbols will give us inheritance (Object -> HashSet) but won't recognize that HashSet implements IEnumerable so we have to check
                // for it specifically.
                var isGenericEnumerable = parameterSymbol.Type.IsOrImplementsOrDerivesFrom(_compilation.GetTypeByMetadataName(Constants.IEnumerableGenericType));

                var isEnumerable = parameterSymbol.Type.IsOrImplementsOrDerivesFrom(_compilation.GetTypeByMetadataName(Constants.IEnumerableType));

                // Check if mapping type
                if (parameterSymbol.Type.IsOrImplementsOrDerivesFrom(_compilation.GetTypeByMetadataName(Constants.IEnumerableOfKeyValuePair))
                    || parameterSymbol.Type.IsOrImplementsOrDerivesFrom(_compilation.GetTypeByMetadataName(Constants.LookupGenericType))
                    || parameterSymbol.Type.IsOrImplementsOrDerivesFrom(_compilation.GetTypeByMetadataName(Constants.DictionaryGenericType)))
                {
                    return false;
                }

                // IEnumerable and not string or dictionary
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
                    else if (isGenericEnumerable) // if this is a generic enumerable but wasn't caught by the previous two cases, we assume it is some IEnumerable<T>
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

                    if (currSymbol.IsOrDerivedFrom(_compilation.GetTypeByMetadataName(Constants.IEnumerableGenericType)) && currSymbol is INamedTypeSymbol currNamedSymbol)
                    {
                        finalSymbol = currNamedSymbol;
                        break;
                    }

                    genericInterfaceSymbol = currSymbol.Interfaces.Where(i => i.IsOrDerivedFrom(_compilation.GetTypeByMetadataName(Constants.IEnumerableGenericType))).FirstOrDefault();
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
                return SymbolEqualityComparer.Default.Equals(symbol, _compilation.GetTypeByMetadataName(Constants.StringType))
                    || (symbol is IArrayTypeSymbol arraySymbol && SymbolEqualityComparer.Default.Equals(arraySymbol.ElementType, _compilation.GetTypeByMetadataName(Constants.StringType)));
            }

            private bool IsBinaryType(ITypeSymbol symbol)
            {
                var isByteArray = SymbolEqualityComparer.Default.Equals(symbol, _compilation.GetTypeByMetadataName(Constants.ByteArrayType))
                    || (symbol is IArrayTypeSymbol arraySymbol && SymbolEqualityComparer.Default.Equals(arraySymbol.ElementType, _compilation.GetTypeByMetadataName(Constants.ByteStructType)));
                var isReadOnlyMemoryOfBytes = SymbolEqualityComparer.Default.Equals(symbol, _compilation.GetTypeByMetadataName(Constants.ReadOnlyMemoryOfBytes));
                var isArrayOfByteArrays = symbol is IArrayTypeSymbol outerArray && 
                    outerArray.ElementType is IArrayTypeSymbol innerArray && SymbolEqualityComparer.Default.Equals(innerArray.ElementType, _compilation.GetTypeByMetadataName(Constants.ByteStructType));


                return isByteArray || isReadOnlyMemoryOfBytes || isArrayOfByteArrays;
            }
        }
    }
}
