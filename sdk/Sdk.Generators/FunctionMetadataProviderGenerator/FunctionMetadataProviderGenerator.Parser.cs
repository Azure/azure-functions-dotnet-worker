// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Azure.Functions.Worker.Sdk.Generators
{
    public partial class FunctionMetadataProviderGenerator
    {
        internal sealed class Parser
        {
            private readonly GeneratorExecutionContext _context;
            private readonly ImmutableArray<string> _functionsStringNamesToRemove;
            private readonly KnownTypes _knownTypes;
            private readonly KnownFunctionMetadataTypes _knownFunctionMetadataTypes;
            private DataTypeParser _dataTypeParser;
            private CardinalityParser _cardinalityParser;

            public Parser(GeneratorExecutionContext context)
            {
                _context = context;
                _functionsStringNamesToRemove = ImmutableArray.Create("Attribute", "Input", "Output");
                _knownTypes = new KnownTypes(context.Compilation);
                _knownFunctionMetadataTypes = new KnownFunctionMetadataTypes(context.Compilation);
                _dataTypeParser = new DataTypeParser(_knownTypes);
                _cardinalityParser = new CardinalityParser(_knownTypes, _knownFunctionMetadataTypes, _dataTypeParser);
            }

            private Compilation Compilation => _context.Compilation;

            private CancellationToken CancellationToken => _context.CancellationToken;

            /// <summary>
            /// Takes in candidate methods from the user compilation and parses them to return function metadata info as GeneratorFunctionMetadata.
            /// </summary>
            /// <param name="methods">List of candidate methods from the syntax receiver.</param>
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

                    if (!FunctionsUtil.IsValidFunctionMethod(_context, Compilation, model, method, out string? functionName))
                    {
                        continue;
                    }

                    var newFunction = new GeneratorFunctionMetadata
                    {
                        Name = functionName,
                        EntryPoint = FunctionsUtil.GetFullyQualifiedMethodName(method, model),
                        Language = Constants.Languages.DotnetIsolated,
                        ScriptFile = scriptFile
                    };

                    if (!TryGetBindings(method, model, out IList<IDictionary<string, object>>? bindings, out bool hasHttpTrigger, out GeneratorRetryOptions? retryOptions))
                    {
                        continue;
                    }

                    if (hasHttpTrigger)
                    {
                        newFunction.IsHttpTrigger = true;
                    }

                    if (retryOptions is not null)
                    {
                        newFunction.Retry = retryOptions;
                    }

                    newFunction.RawBindings = bindings!; // won't be null b/c TryGetBindings would've failed and this line wouldn't be reached

                    result.Add(newFunction);
                }

                return result;
            }

            private bool TryGetBindings(MethodDeclarationSyntax method, SemanticModel model, out IList<IDictionary<string, object>>? bindings, out bool hasHttpTrigger, out GeneratorRetryOptions? validatedRetryOptions)
            {
                hasHttpTrigger = false;
                validatedRetryOptions = null;

                if (!TryGetMethodOutputBinding(method, model, out bool hasOutputBinding, out GeneratorRetryOptions? retryOptions, out IList<IDictionary<string, object>>? methodOutputBindings)
                    || !TryGetParameterInputAndTriggerBindings(method, model, out bool supportsRetryOptions, out hasHttpTrigger, out IList<IDictionary<string, object>>? parameterInputAndTriggerBindings)
                    || !TryGetReturnTypeBindings(method, model, hasHttpTrigger, hasOutputBinding, out IList<IDictionary<string, object>>? returnTypeBindings))
                {
                    bindings = null;
                    return false;
                }

                var listSize = methodOutputBindings!.Count + parameterInputAndTriggerBindings!.Count + returnTypeBindings!.Count;
                var result = new List<IDictionary<string, object>>(listSize);

                result.AddRange(methodOutputBindings);
                result.AddRange(parameterInputAndTriggerBindings);
                result.AddRange(returnTypeBindings);
                bindings = result;

                if (retryOptions is not null)
                {
                    if (supportsRetryOptions && retryOptions is not null)
                    {
                        validatedRetryOptions = retryOptions;
                    }
                    else if (!supportsRetryOptions && retryOptions is not null)
                    {
                        _context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.InvalidRetryOptions, method.GetLocation()));
                        return false;
                    }
                }

                return true;
            }

            /// <summary>
            /// Checks for and returns any OutputBinding attributes associated with the method.
            /// </summary>
            private bool TryGetMethodOutputBinding(MethodDeclarationSyntax method, SemanticModel model, out bool hasOutputBinding, out GeneratorRetryOptions? retryOptions, out IList<IDictionary<string, object>>? bindingsList)
            {
                var bindingLocation = method.Identifier.GetLocation();

                var methodSymbol = model.GetDeclaredSymbol(method);
                var attributes = methodSymbol!.GetAttributes(); // methodSymbol is not null here because it's checked in IsValidAzureFunction which is called before bindings are collected/created

                AttributeData? outputBindingAttribute = null;
                hasOutputBinding = false;
                retryOptions = null;

                foreach (var attribute in attributes)
                {
                    if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass?.BaseType, _knownFunctionMetadataTypes.RetryAttribute))
                    {
                        if (TryGetRetryOptionsFromAtttribute(attribute, method.GetLocation(), out GeneratorRetryOptions? retryOptionsFromAttr))
                        {
                            retryOptions = retryOptionsFromAttr;
                        }
                    }

                    if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass?.BaseType, _knownFunctionMetadataTypes.OutputBindingAttribute))
                    {
                        // There can only be one output binding associated with a function. If there is more than one, we return a diagnostic error here.
                        if (hasOutputBinding)
                        {
                            _context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.MultipleBindingsGroupedTogether, bindingLocation, new object[] { "Method", method.Identifier.ToString() }));
                            bindingsList = null;
                            return false;
                        }

                        outputBindingAttribute = attribute;
                        hasOutputBinding = true;
                    }
                }

                if (outputBindingAttribute != null)
                {
                    if (!TryCreateBindingDict(outputBindingAttribute, Constants.FunctionMetadataBindingProps.ReturnBindingName, bindingLocation, out IDictionary<string, object>? bindingDict))
                    {
                        bindingsList = null;
                        return false;
                    }

                    bindingsList = new List<IDictionary<string, object>>(capacity: 1)
                    {
                        bindingDict!
                    };

                    return true;
                }

                // we didn't find any output bindings on the method, but there were no errors
                // so we treat the found bindings as an empty list and return true
                bindingsList = new List<IDictionary<string, object>>();
                return true;
            }

            private bool TryGetRetryOptionsFromAtttribute(AttributeData attribute, Location location, out GeneratorRetryOptions? retryOptions)
            {
                retryOptions = null;

                if (TryGetAttributeProperties(attribute, null, out IDictionary<string, object?>? attrProperties))
                {
                    retryOptions = new GeneratorRetryOptions();

                    // Would not expect this to fail since MaxRetryCount is a required value of a retry policy attribute
                    if (attrProperties!.TryGetValue(Constants.RetryConstants.MaxRetryCountKey, out object? maxRetryCount))
                    {
                        retryOptions.MaxRetryCount = maxRetryCount!.ToString();
                    }

                    // Check which strategy is being used by checking the attribute class
                    if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, _knownFunctionMetadataTypes.FixedDelayRetryAttribute))
                    {
                        retryOptions.Strategy = RetryStrategy.FixedDelay;

                        if (attrProperties.TryGetValue(Constants.RetryConstants.DelayIntervalKey, out object? delayInterval)) // nonnullable constructor arg of attribute, wouldn't expect this to fail
                        {
                            retryOptions.DelayInterval = delayInterval!.ToString();
                        }

                    }
                    else if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, _knownFunctionMetadataTypes.ExponentialBackoffRetryAttribute))
                    {
                        retryOptions.Strategy = RetryStrategy.ExponentialBackoff;

                        if (attrProperties.TryGetValue(Constants.RetryConstants.MinimumIntervalKey, out object? minimumInterval)) // nonnullable constructor arg of attribute, wouldn't expect this to fail
                        {
                            retryOptions.MinimumInterval = minimumInterval!.ToString();
                        }

                        if (attrProperties.TryGetValue(Constants.RetryConstants.MaximumIntervalKey, out object? maximumInterval)) // nonnullable constructor arg of attribute, wouldn't expect this to fail
                        {
                            retryOptions.MinimumInterval = maximumInterval!.ToString();
                        }
                    }
                    else
                    {
                        _context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.InvalidRetryOptions, location));
                        return false;
                    }

                    return true;
                }

                return false;
            }

            /// <summary>
            /// Checks for and returns input and trigger bindings found in the parameters of the Azure Function method.
            /// </summary>
            private bool TryGetParameterInputAndTriggerBindings(MethodDeclarationSyntax method, SemanticModel model, out bool supportsRetryOptions, out bool hasHttpTrigger, out IList<IDictionary<string, object>>? bindingsList)
            {
                supportsRetryOptions = false;
                hasHttpTrigger = false;
                bindingsList = new List<IDictionary<string, object>>();

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
                        if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass?.BaseType?.BaseType, _knownFunctionMetadataTypes.BindingAttribute))
                        {

                            if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, _knownFunctionMetadataTypes.HttpTriggerBinding))
                            {
                                hasHttpTrigger = true;
                            }

                            DataType dataType = _dataTypeParser.GetDataType(parameterSymbol.Type);

                            bool cardinalityValidated = false;

                            if (_cardinalityParser.IsCardinalitySupported(attribute))
                            {
                                DataType updatedDataType = DataType.Undefined;

                                if (!_cardinalityParser.IsCardinalityValid(parameterSymbol, parameter.Type, attribute, out updatedDataType))
                                {
                                    _context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.InvalidCardinality, parameter.Identifier.GetLocation(), parameterSymbol.Name));
                                    bindingsList = null;
                                    return false;
                                }

                                // update the DataType of this binding with the updated type found during call to IsCardinalityValid
                                // ex. IList<String> would be evaluated as "Undefined" by the call to GetDataType
                                // but it would be correctly evaluated as "String" during the call to IsCardinalityValid which parses iterable collections
                                dataType = updatedDataType;
                                cardinalityValidated = true;
                            }

                            string bindingName = parameter.Identifier.ValueText;

                            if (!TryCreateBindingDict(attribute, bindingName, parameter.Identifier.GetLocation(), out IDictionary<string, object>? bindingDict))
                            {
                                bindingsList = null;
                                return false;
                            }

                            // If cardinality is supported and validated, but was not found in named args, constructor args, or default value attributes
                            // default to Cardinality: One to stay in sync with legacy generator.
                            if (cardinalityValidated && !bindingDict!.Keys.Contains("cardinality"))
                            {
                                bindingDict!.Add("cardinality", "One");
                            }

                            if (dataType is not DataType.Undefined)
                            {
                                bindingDict!.Add("dataType", Enum.GetName(typeof(DataType), dataType));
                            }

                            // check for binding capabilities
                            var bindingCapabilitiesAttr = attribute?.AttributeClass?.GetAttributes().Where(a => (SymbolEqualityComparer.Default.Equals(a.AttributeClass, _knownFunctionMetadataTypes.BindingCapabilitiesAttribute)));
                            if (bindingCapabilitiesAttr.FirstOrDefault() is not null)
                            {
                                var bindingCapabilities = bindingCapabilitiesAttr.FirstOrDefault().ConstructorArguments;

                                if (bindingCapabilities.Any(s => string.Equals(s.Values.FirstOrDefault().Value?.ToString(), Constants.BindingCapabilities.FunctionLevelRetry, StringComparison.OrdinalIgnoreCase)))
                                {
                                    supportsRetryOptions = true;
                                }
                            }

                            bindingsList.Add(bindingDict!);
                        }
                    }
                }

                return true;
            }

            /// <summary>
            /// Checks for and returns any bindings found in the Return Type of the method
            /// </summary>
            private bool TryGetReturnTypeBindings(MethodDeclarationSyntax method, SemanticModel model, bool hasHttpTrigger, bool hasOutputBinding, out IList<IDictionary<string, object>>? bindingsList)
            {
                TypeSyntax returnTypeSyntax = method.ReturnType;
                ITypeSymbol? returnTypeSymbol = model.GetSymbolInfo(returnTypeSyntax).Symbol as ITypeSymbol;
                bindingsList = new List<IDictionary<string, object>>();

                if (returnTypeSymbol is null)
                {
                    _context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.SymbolNotFound, returnTypeSyntax.GetLocation(), nameof(returnTypeSymbol)));
                    bindingsList = null;
                    return false;
                }

                if (!SymbolEqualityComparer.Default.Equals(returnTypeSymbol, _knownTypes.VoidType) &&
                    !SymbolEqualityComparer.Default.Equals(returnTypeSymbol, _knownTypes.TaskType))
                {
                    // If there is a Task<T> return type, inspect T, the inner type.
                    if (SymbolEqualityComparer.Default.Equals(returnTypeSymbol, _knownTypes.TaskOfTType))
                    {
                        GenericNameSyntax genericSyntax = (GenericNameSyntax)returnTypeSyntax;
                        var innerTypeSyntax = genericSyntax.TypeArgumentList.Arguments.First(); // Generic task should only have one type argument
                        returnTypeSymbol = model.GetSymbolInfo(innerTypeSyntax).Symbol as ITypeSymbol;

                        if (returnTypeSymbol is null) // need this check here b/c return type symbol takes on a new value from the inner argument type above
                        {
                            _context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.SymbolNotFound, genericSyntax.Identifier.GetLocation(), nameof(returnTypeSymbol)));
                            bindingsList = null;
                            return false;
                        }
                    }

                    if (SymbolEqualityComparer.Default.Equals(returnTypeSymbol, _knownFunctionMetadataTypes.HttpResponse)) // If return type is HttpResponseData
                    {
                        bindingsList.Add(GetHttpReturnBinding(Constants.FunctionMetadataBindingProps.ReturnBindingName));
                    }
                    else
                    {
                        if (!TryGetReturnTypePropertyBindings(returnTypeSymbol, hasHttpTrigger, hasOutputBinding, returnTypeSyntax.GetLocation(), out bindingsList))
                        {
                            bindingsList = null;
                            return false;
                        }
                    }
                }

                return true;
            }

            private bool TryGetReturnTypePropertyBindings(ITypeSymbol returnTypeSymbol, bool hasHttpTrigger, bool hasOutputBinding, Location returnTypeLocation, out IList<IDictionary<string, object>>? bindingsList)
            {
                var members = returnTypeSymbol.GetMembers();
                var foundHttpOutput = false;
                bindingsList = new List<IDictionary<string, object>>(); // initialize this without size, because it will be difficult to predict how many bindings we can find here in the user code.

                foreach (var prop in returnTypeSymbol.GetMembers().Where(a => a is IPropertySymbol))
                {
                    if (prop is null)
                    {
                        _context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.SymbolNotFound, Location.None, nameof(prop)));
                        bindingsList = null;
                        return false;
                    }

                    // Check if this attribute is an HttpResponseData type attribute
                    if (prop is IPropertySymbol property && SymbolEqualityComparer.Default.Equals(property.Type, _knownFunctionMetadataTypes.HttpResponse))
                    {
                        if (foundHttpOutput)
                        {
                            _context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.MultipleHttpResponseTypes, returnTypeLocation, new object[] { returnTypeSymbol.Name }));
                            bindingsList = null;
                            return false;
                        }

                        foundHttpOutput = true;
                        bindingsList.Add(GetHttpReturnBinding(prop.Name));
                    }
                    else if (prop.GetAttributes().Length > 0) // check if this property has any attributes
                    {
                        var foundPropertyOutputAttr = false;

                        foreach (var attr in prop.GetAttributes()) // now loop through and check if any of the attributes are Binding attributes
                        {
                            if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass?.BaseType, _knownFunctionMetadataTypes.OutputBindingAttribute))
                            {
                                // validate that there's only one binding attribute per property
                                if (foundPropertyOutputAttr)
                                {
                                    _context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.MultipleBindingsGroupedTogether, prop.Locations.FirstOrDefault(), new object[] { "Property", prop.Name.ToString() }));
                                    bindingsList = null;
                                    return false;
                                }

                                if (!TryCreateBindingDict(attr, prop.Name, prop.Locations.FirstOrDefault(), out IDictionary<string, object>? bindingDict))
                                {
                                    bindingsList = null;
                                    return false;
                                }

                                bindingsList.Add(bindingDict!);

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
                        bindingsList.Add(GetHttpReturnBinding(Constants.FunctionMetadataBindingProps.ReturnBindingName));
                    }
                    else
                    {
                        bindingsList.Add(GetHttpReturnBinding(Constants.FunctionMetadataBindingProps.HttpResponseBindingName));
                    }
                }

                return true;
            }

            private IDictionary<string, object> GetHttpReturnBinding(string returnBindingName)
            {
                var httpBinding = new Dictionary<string, object>
                {
                    { "name", returnBindingName },
                    { "type", "http" },
                    { "direction", "Out" }
                };

                return httpBinding;
            }

            private bool TryCreateBindingDict(AttributeData bindingAttrData, string bindingName, Location? bindingLocation, out IDictionary<string, object>? bindings)
            {
                // Get binding info as a dictionary with keys as the property name and value as the property value
                if (!TryGetAttributeProperties(bindingAttrData, bindingLocation, out IDictionary<string, object?>? attributeProperties))
                {
                    bindings = null;
                    return false;
                }

                // Grab some required binding info properties
                string attributeName = bindingAttrData.AttributeClass!.Name;

                // properly format binding types by removing "Attribute" and "Input" descriptors
                string bindingType = attributeName.TrimStringsFromEnd(_functionsStringNamesToRemove);

                // Set binding direction
                string bindingDirection = SymbolEqualityComparer.Default.Equals(bindingAttrData.AttributeClass?.BaseType, _knownFunctionMetadataTypes.OutputBindingAttribute) ? "Out" : "In";

                var bindingCount = attributeProperties!.Count + 3;
                bindings = new Dictionary<string, object>(capacity: bindingCount)
                {
                    { "name", bindingName },
                    { "type", bindingType },
                    { "direction", bindingDirection }
                };

                // Add additional bindingInfo to the anonymous type because some functions have more properties than others
                foreach (var prop in attributeProperties!) // attributeProperties won't be null here b/c we would've exited this method earlier if it was during TryGetAttributeProperties check
                {
                    var propertyName = prop.Key;

                    if (prop.Value?.GetType().IsArray ?? false)
                    {
                        bindings[propertyName] = prop.Value;
                    }
                    else
                    {
                        bindings[propertyName] = prop.Value!.ToString();
                    }
                }

                return true;
            }

            private bool TryGetAttributeProperties(AttributeData attributeData, Location? attribLocation, out IDictionary<string, object?>? attrProperties)
            {
                attrProperties = new Dictionary<string, object?>();

                if (attributeData.ConstructorArguments.Any())
                {
                    if (!TryLoadConstructorArguments(attributeData, attrProperties, attribLocation))
                    {
                        attrProperties = null;
                        return false;
                    }
                }

                foreach (var namedArgument in attributeData.NamedArguments)
                {
                    if (namedArgument.Value.Value != null)
                    {
                        if (string.Equals(namedArgument.Key, Constants.FunctionMetadataBindingProps.IsBatchedKey) && !attrProperties.ContainsKey("cardinality"))
                        {
                            var argValue = (bool)namedArgument.Value.Value; // isBatched only takes in booleans and the generator will parse it as a bool so we can type cast this to use in the next line

                            attrProperties["cardinality"] = argValue ? "Many" : "One";
                        }
                        else
                        {
                            attrProperties[namedArgument.Key] = namedArgument.Value.Value;
                        }
                    }
                }

                // some properties have default values, so if these properties were not already defined in constructor or named arguments, we will auto-add them here
                foreach (var member in attributeData.AttributeClass!.GetMembers().Where(a => a is IPropertySymbol))
                {
                    var defaultValAttrList = member.GetAttributes().Where(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, _knownFunctionMetadataTypes.DefaultValue));

                    if (defaultValAttrList.SingleOrDefault() is { } defaultValAttr) // list will only be of size one b/c there cannot be duplicates of an attribute on one piece of syntax
                    {
                        var argName = member.Name;
                        object arg = defaultValAttr.ConstructorArguments.SingleOrDefault().Value!; // only one constructor arg in DefaultValue attribute (the default value)
                        if (arg is bool b && string.Equals(argName, Constants.FunctionMetadataBindingProps.IsBatchedKey))
                        {
                            if (!attrProperties.Keys.Contains("cardinality"))
                            {
                                attrProperties["cardinality"] = b ? "Many" : "One";
                            }
                        }
                        else if (!attrProperties.Keys.Any(a => string.Equals(a, argName, StringComparison.OrdinalIgnoreCase))) // check if this property has been assigned a value already in constructor or named args
                        {
                            attrProperties[argName] = arg;
                        }
                    }
                }

                return true;
            }

            private bool TryLoadConstructorArguments(AttributeData attributeData, IDictionary<string, object?> dict, Location? attribLocation)
            {
                IMethodSymbol? attribMethodSymbol = attributeData.AttributeConstructor;

                // Check if the attribute constructor has any parameters
                if (attribMethodSymbol is null)
                {
                    _context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.SymbolNotFound, attribLocation, nameof(attribMethodSymbol)));
                    return false;
                }

                // It's fair to assume that constructor arguments appear before named arguments, and
                // that the constructor names would match the property names
                for (int i = 0; i < attributeData.ConstructorArguments.Length; i++)
                {
                    string argumentName = attribMethodSymbol.Parameters[i].Name;
                    OverrideBindingName(attributeData.AttributeClass!, ref argumentName); // either argumentName will remain unchanged OR be updated to the overridden name at the end of this.

                    var arg = attributeData.ConstructorArguments[i];

                    switch (arg.Kind)
                    {
                        case TypedConstantKind.Error:
                            break;

                        case TypedConstantKind.Primitive:
                            dict[argumentName] = arg.Value;
                            break;

                        case TypedConstantKind.Enum:
                            var enumValue = arg.Type!.GetMembers()
                                .FirstOrDefault(m => m is IFieldSymbol field
                                    && field.ConstantValue is object value
                                    && value.Equals(arg.Value));

                            if (enumValue is null)
                            {
                                return false;
                            }

                            // we want just the enumValue symbol's name (Admin, Anonymous, Function)
                            dict[argumentName] = enumValue.Name;
                            break;

                        case TypedConstantKind.Type:
                            break;

                        case TypedConstantKind.Array:
                            var arrayValues = arg.Values.Select(a => a.Value?.ToString()).ToArray();
                            dict[argumentName] = arrayValues;
                            break;

                        default:
                            break;
                    }
                }

                return true;
            }

            /// <summary>
            /// This method handles cases where an attribute property has a different function metadata binding name.
            /// </summary>
            /// <remarks>
            /// For example, in the BlobTriggerAttribute type, the "BlobPath" property is decorated with "MetadataBindingPropertyName" attribute
            /// where "path" is provided as the value to be used when generating metadata binding data, as shown below.
            ///
            ///     [MetadataBindingPropertyName("path")]
            ///     public string BlobPath { get; set; }
            ///
            /// </remarks>
            /// <param name="attributeClass">The attribute type represented as an <see cref="INamedTypeSymbol"/></param>
            /// <param name="argumentName">The argument's name as represented in the constructor. This may be overriden by the MetadataBindingPropertyName.</param>
            private void OverrideBindingName(INamedTypeSymbol attributeClass, ref string argumentName)
            {
                foreach (var prop in attributeClass.GetMembers().Where(a => a is IPropertySymbol))
                {
                    if (String.Equals(prop.Name, argumentName, StringComparison.OrdinalIgnoreCase)) // relies on convention where constructor parameter names match the property their value will be assigned to (JSON serialization is a precedence for this convention)
                    {
                        var bindingNameAttrList = prop.GetAttributes().Where(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, _knownFunctionMetadataTypes.BindingPropertyNameAttribute));

                        if (bindingNameAttrList.SingleOrDefault() is { } bindingNameAttr) // there will only be one BindingAttributeName attribute b/c there can't be duplicate attributes on a piece of syntax
                        {
                            argumentName = bindingNameAttr.ConstructorArguments.First().Value!.ToString(); // there is only one constructor argument for this binding attribute (the binding name override)
                        }
                    }
                }
            }
        }
    }
}
