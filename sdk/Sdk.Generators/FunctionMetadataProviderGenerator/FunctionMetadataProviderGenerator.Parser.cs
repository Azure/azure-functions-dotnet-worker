// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

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
            /// <param name="parsingContext">An instance of <see cref="FunctionsMetadataParsingContext"/>. Optional.</param>
            public IReadOnlyList<GeneratorFunctionMetadata> GetFunctionMetadataInfo(List<IMethodSymbol> methods, FunctionsMetadataParsingContext? parsingContext = null)
            {
                var result = ImmutableArray.CreateBuilder<GeneratorFunctionMetadata>();

                // Loop through the candidate methods (methods with any attribute associated with them) which are public.
                foreach (IMethodSymbol method in methods.Where(m => m.DeclaredAccessibility == Accessibility.Public))
                {
                    CancellationToken.ThrowIfCancellationRequested();

                    if (!FunctionsUtil.TryGetFunctionName(method, Compilation, out var funcName))
                    {
                        _context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.SymbolNotFound, Location.None, method.Name)); // would only reach here if the function attribute or method was not loaded, resulting in failure to retrieve name
                    }

                    var assemblyName = method.ContainingAssembly.Name;
                    var scriptFile = $"{assemblyName}{parsingContext?.ScriptFileExtension ?? ".dll"}";

                    var newFunction = new GeneratorFunctionMetadata
                    {
                        Name = funcName,
                        EntryPoint = FunctionsUtil.GetFullyQualifiedMethodName(method),
                        Language = Constants.Languages.DotnetIsolated,
                        ScriptFile = scriptFile
                    };

                    if (!TryGetBindings(method, out IList<IDictionary<string, object>>? bindings, out bool hasHttpTrigger, out GeneratorRetryOptions? retryOptions))
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

            private bool TryGetBindings(IMethodSymbol method, out IList<IDictionary<string, object>>? bindings, out bool hasHttpTrigger, out GeneratorRetryOptions? validatedRetryOptions)
            {
                hasHttpTrigger = false;
                validatedRetryOptions = null;

                if (!TryGetMethodOutputBinding(method, out bool hasMethodOutputBinding, out GeneratorRetryOptions? retryOptions, out IList<IDictionary<string, object>>? methodOutputBindings)
                    || !TryGetParameterInputAndTriggerBindings(method, out bool supportsRetryOptions, out hasHttpTrigger, out IList<IDictionary<string, object>>? parameterInputAndTriggerBindings)
                    || !TryGetReturnTypeBindings(method, hasHttpTrigger, hasMethodOutputBinding, out IList<IDictionary<string, object>>? returnTypeBindings))
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
                    if (supportsRetryOptions)
                    {
                        validatedRetryOptions = retryOptions;
                    }
                    else if (!supportsRetryOptions)
                    {
                        _context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.InvalidRetryOptions, Location.None));
                        return false;
                    }
                }

                return true;
            }

            /// <summary>
            /// Checks for and returns any OutputBinding attributes associated with the method.
            /// </summary>
            private bool TryGetMethodOutputBinding(IMethodSymbol method, out bool hasMethodOutputBinding, out GeneratorRetryOptions? retryOptions, out IList<IDictionary<string, object>>? bindingsList)
            {
                var attributes = method!.GetAttributes(); // methodSymbol is not null here because it's checked in IsValidAzureFunction which is called before bindings are collected/created

                AttributeData? outputBindingAttribute = null;
                hasMethodOutputBinding = false;
                retryOptions = null;

                foreach (var attribute in attributes)
                {
                    if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass?.BaseType, _knownFunctionMetadataTypes.RetryAttribute))
                    {
                        if (TryGetRetryOptionsFromAttribute(attribute, Location.None, out GeneratorRetryOptions? retryOptionsFromAttr))
                        {
                            retryOptions = retryOptionsFromAttr;
                        }
                    }

                    if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass?.BaseType, _knownFunctionMetadataTypes.OutputBindingAttribute))
                    {
                        // There can only be one method output binding associated with a function. If there is more than one, we return a diagnostic error here.
                        if (hasMethodOutputBinding)
                        {
                            _context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.MultipleBindingsGroupedTogether, Location.None, new object[] { "Method", method.Name }));
                            bindingsList = null;
                            return false;
                        }

                        outputBindingAttribute = attribute;
                        hasMethodOutputBinding = true;
                    }
                }

                if (outputBindingAttribute != null)
                {
                    if (!TryCreateBindingDictionary(outputBindingAttribute, Constants.FunctionMetadataBindingProps.ReturnBindingName, Location.None, out IDictionary<string, object>? bindings))
                    {
                        bindingsList = null;
                        return false;
                    }

                    bindingsList = new List<IDictionary<string, object>>(capacity: 1)
                    {
                        bindings!
                    };

                    return true;
                }

                // we didn't find any output bindings on the method, but there were no errors
                // so we treat the found bindings as an empty list and return true
                bindingsList = new List<IDictionary<string, object>>();
                return true;
            }

            private bool TryGetRetryOptionsFromAttribute(AttributeData attribute, Location location, out GeneratorRetryOptions? retryOptions)
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
                            retryOptions.MaximumInterval = maximumInterval!.ToString();
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
            private bool TryGetParameterInputAndTriggerBindings(IMethodSymbol method, out bool supportsRetryOptions, out bool hasHttpTrigger, out IList<IDictionary<string, object>>? bindingsList)
            {
                supportsRetryOptions = false;
                hasHttpTrigger = false;
                bindingsList = new List<IDictionary<string, object>>();

                foreach (IParameterSymbol parameter in method.Parameters)
                {
                    // If there's no attribute, we can assume that this parameter is not a binding
                    if (!parameter.GetAttributes().Any())
                    {
                        continue;
                    }

                    // Check to see if any of the attributes associated with this parameter is a BindingAttribute
                    foreach (var attribute in parameter.GetAttributes())
                    {
                        if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass?.BaseType?.BaseType, _knownFunctionMetadataTypes.BindingAttribute))
                        {

                            if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, _knownFunctionMetadataTypes.HttpTriggerBinding))
                            {
                                hasHttpTrigger = true;
                            }

                            DataType dataType = _dataTypeParser.GetDataType(parameter.Type);

                            bool cardinalityValidated = false;
                            bool supportsDeferredBinding = SupportsDeferredBinding(attribute, parameter.Type.ToString());

                            if (_cardinalityParser.IsCardinalitySupported(attribute))
                            {
                                DataType updatedDataType = DataType.Undefined;

                                if (!_cardinalityParser.IsCardinalityValid(parameter, attribute, out updatedDataType))
                                {
                                    _context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.InvalidCardinality, Location.None, parameter.Name));
                                    bindingsList = null;
                                    return false;
                                }

                                // update the DataType of this binding with the updated type found during call to IsCardinalityValid
                                // ex. IList<String> would be evaluated as "Undefined" by the call to GetDataType
                                // but it would be correctly evaluated as "String" during the call to IsCardinalityValid which parses iterable collections
                                dataType = updatedDataType;
                                cardinalityValidated = true;
                            }

                            string bindingName = parameter.Name;

                            if (!TryCreateBindingDictionary(attribute, bindingName, Location.None, out IDictionary<string, object>? bindings, supportsDeferredBinding))
                            {
                                bindings = null;
                                return false;
                            }

                            // If cardinality is supported and validated, but was not found in named args, constructor args, or default value attributes
                            // default to Cardinality: One to stay in sync with legacy generator.
                            if (cardinalityValidated && !bindings!.Keys.Contains("cardinality"))
                            {
                                bindings!.Add("cardinality", "One");
                            }

                            if (dataType is not DataType.Undefined)
                            {
                                bindings!.Add("dataType", Enum.GetName(typeof(DataType), dataType));
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

                            bindingsList.Add(bindings!);
                        }
                    }
                }

                return true;
            }

            private bool SupportsDeferredBinding(AttributeData bindingAttribute, string bindingType)
            {
                var advertisedAttributes = bindingAttribute?.AttributeClass?.GetAttributes();

                if (advertisedAttributes != null)
                {
                    foreach (var advertisedAttribute in advertisedAttributes)
                    {
                        if (SymbolEqualityComparer.Default.Equals(advertisedAttribute.AttributeClass, _knownFunctionMetadataTypes.InputConverterAttributeType))
                        {
                            foreach (var converter in advertisedAttribute.ConstructorArguments)
                            {
                                if (DoesConverterSupportDeferredBinding(converter, bindingType))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
                return false;
            }

            private bool DoesConverterSupportDeferredBinding(TypedConstant converter, string bindingType)
            {
                var converterType = converter.Value as ITypeSymbol;
                var converterAdvertisedAttributes = converterType?.GetAttributes().ToList();

                if (converterAdvertisedAttributes is not null)
                {
                    bool converterAdvertisesDeferredBindingSupport = converterAdvertisedAttributes.Any(
                        a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, _knownFunctionMetadataTypes.SupportsDeferredBindingAttributeType));

                    if (converterAdvertisesDeferredBindingSupport)
                    {
                        bool converterAdvertisesTypes = converterAdvertisedAttributes.Any(
                            a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, _knownFunctionMetadataTypes.SupportedTargetTypeAttributeType));

                        if (!converterAdvertisesTypes)
                        {
                            // If a converter advertises deferred binding but does not explicitly advertise any types then DeferredBinding will be supported for all the types
                            return true;
                        }

                        return DoesConverterSupportTargetType(converterAdvertisedAttributes, bindingType);
                    }
                }

                return false;
            }

            private bool DoesConverterSupportTargetType(List<AttributeData> converterAdvertisedAttributes, string bindingType)
            {
                foreach (AttributeData attribute in converterAdvertisedAttributes)
                {
                    if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, _knownFunctionMetadataTypes.SupportedTargetTypeAttributeType))
                    {
                        foreach (var element in attribute.ConstructorArguments)
                        {
                            if (string.Equals(element.Type?.GetFullName(), typeof(Type).FullName, StringComparison.Ordinal)
                                && string.Equals(element.Value?.ToString(), bindingType, StringComparison.Ordinal))
                            {
                                return true;
                            }
                        }
                    }
                }

                return false;
            }

            /// <summary>
            /// Checks for and returns any bindings found in the Return Type of the method
            /// </summary>
            private bool TryGetReturnTypeBindings(IMethodSymbol method, bool hasHttpTrigger, bool hasMethodOutputBinding, out IList<IDictionary<string, object>>? bindingsList)
            {
                ITypeSymbol? returnTypeSymbol = method.ReturnType;
                bindingsList = new List<IDictionary<string, object>>();

                if (returnTypeSymbol is null)
                {
                    _context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.SymbolNotFound, Location.None, nameof(returnTypeSymbol)));
                    bindingsList = null;
                    return false;
                }

                if (!SymbolEqualityComparer.Default.Equals(returnTypeSymbol, _knownTypes.VoidType) &&
                    !SymbolEqualityComparer.Default.Equals(returnTypeSymbol.OriginalDefinition, _knownTypes.TaskType) ||
                    // For HTTP triggers, include the return binding even if the return type is void or Task.
                    hasHttpTrigger)
                {
                    // If there is a Task<T> return type, inspect T, the inner type.
                    if (SymbolEqualityComparer.Default.Equals(returnTypeSymbol.OriginalDefinition, _knownTypes.TaskOfTType))
                    {
                        if (returnTypeSymbol is INamedTypeSymbol namedTypeSymbol)
                        {
                            if (namedTypeSymbol.IsGenericType)
                            {
                                returnTypeSymbol = namedTypeSymbol.TypeArguments.FirstOrDefault();// Generic task should only have one type argument
                            }
                        }

                        if (returnTypeSymbol is null) // need this check here b/c return type symbol takes on a new value from the inner argument type above
                        {
                            _context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.SymbolNotFound, Location.None, nameof(returnTypeSymbol)));
                            bindingsList = null;
                            return false;
                        }
                    }

                    if (SymbolEqualityComparer.Default.Equals(returnTypeSymbol, _knownFunctionMetadataTypes.HttpResponseData))
                    {
                        bindingsList.Add(GetHttpReturnBinding(Constants.FunctionMetadataBindingProps.ReturnBindingName));
                    }
                    else
                    {
                        if (!TryGetReturnTypePropertyBindings(returnTypeSymbol, hasHttpTrigger, hasMethodOutputBinding, out bindingsList))
                        {
                            bindingsList = null;
                            return false;
                        }
                    }
                }

                return true;
            }

            private bool TryGetReturnTypePropertyBindings(ITypeSymbol returnTypeSymbol, bool hasHttpTrigger, bool hasMethodOutputBinding, out IList<IDictionary<string, object>>? bindingsList)
            {
                var members = returnTypeSymbol.GetMembers();
                var foundHttpOutput = false;
                var returnTypeHasOutputBindings = false;
                bindingsList = new List<IDictionary<string, object>>(); // initialize this without size, because it will be difficult to predict how many bindings we can find here in the user code.

                foreach (var prop in returnTypeSymbol.GetMembers().Where(a => a is IPropertySymbol))
                {
                    if (prop is null)
                    {
                        _context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.SymbolNotFound, Location.None, nameof(prop)));
                        bindingsList = null;
                        return false;
                    }

                    // Check for HttpResponseData type for legacy apps
                    if (prop is IPropertySymbol property
                             && (SymbolEqualityComparer.Default.Equals(property.Type, _knownFunctionMetadataTypes.HttpResponseData)))
                    {
                        if (foundHttpOutput)
                        {
                            _context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.MultipleHttpResponseTypes, Location.None, new object[] { returnTypeSymbol.Name }));
                            bindingsList = null;
                            return false;
                        }

                        foundHttpOutput = true;
                        bindingsList.Add(GetHttpReturnBinding(prop.Name));
                        continue;
                    }

                    var propAttributes = prop.GetAttributes();

                    if (propAttributes.Length > 0)
                    {
                        var bindingAttributes = propAttributes.Where(p => p.AttributeClass!.IsOrDerivedFrom(_knownFunctionMetadataTypes.BindingAttribute));

                        if (bindingAttributes.Count() > 1)
                        {
                            _context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.MultipleBindingsGroupedTogether, Location.None, new object[] { "Property", prop.Name.ToString() }));
                            bindingsList = null;
                            return false;
                        }

                        // Check if this property has an HttpResultAttribute on it
                        if (HasHttpResultAttribute(prop))
                        {
                            if (foundHttpOutput)
                            {
                                _context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.MultipleHttpResponseTypes, Location.None, new object[] { returnTypeSymbol.Name }));
                                bindingsList = null;
                                return false;
                            }

                            foundHttpOutput = true;
                            bindingsList.Add(GetHttpReturnBinding(prop.Name));
                        }
                        else if (bindingAttributes.Any())
                        {
                            if (!TryCreateBindingDictionary(bindingAttributes.FirstOrDefault(), prop.Name, prop.Locations.FirstOrDefault(), out IDictionary<string, object>? bindings))
                            {
                                bindingsList = null;
                                return false;
                            }

                            bindingsList.Add(bindings!);

                            returnTypeHasOutputBindings = true;
                        }
                    }
                }

                if (hasHttpTrigger && !foundHttpOutput && !hasMethodOutputBinding && !returnTypeHasOutputBindings)
                {
                    bindingsList.Add(GetHttpReturnBinding(Constants.FunctionMetadataBindingProps.ReturnBindingName));
                }

                return true;
            }

            private bool HasHttpResultAttribute(ISymbol prop)
            {
                var attributes = prop.GetAttributes();
                foreach (var attribute in attributes)
                {
                    if (attribute.AttributeClass is not null &&
                        attribute.AttributeClass.IsOrDerivedFrom(_knownFunctionMetadataTypes.HttpResultAttribute))
                    {
                        return true;
                    }
                }

                return false;
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

            private bool TryCreateBindingDictionary(AttributeData bindingAttrData, string bindingName, Location? bindingLocation, out IDictionary<string, object>? bindings, bool supportsDeferredBinding = false)
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
                string bindingType = attributeName.TrimStringsFromEnd(_functionsStringNamesToRemove).ToLowerFirstCharacter();

                // Set binding direction
                string bindingDirection = SymbolEqualityComparer.Default.Equals(bindingAttrData.AttributeClass?.BaseType, _knownFunctionMetadataTypes.OutputBindingAttribute) ? "Out" : "In";

                var bindingCount = attributeProperties!.Count + 3;
                bindings = new Dictionary<string, object>(capacity: bindingCount)
                {
                    { "name", bindingName },
                    { "type", bindingType },
                    { "direction", bindingDirection }
                };

                if (supportsDeferredBinding)
                {
                    bindings.Add("properties", new Dictionary<string, string>() { { "SupportsDeferredBinding", "True" } });
                }

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
                        bindings[propertyName] = prop.Value!;
                    }
                }

                return true;
            }

            private bool TryGetAttributeProperties(AttributeData attributeData, Location? attribLocation, out IDictionary<string, object?>? attrProperties)
            {
                attrProperties = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

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
                    if (IsArrayOrNotNull(namedArgument.Value))
                    {
                        if (string.Equals(namedArgument.Key, Constants.FunctionMetadataBindingProps.IsBatchedKey)
                            && !attrProperties.ContainsKey("cardinality") && namedArgument.Value.Value != null)
                        {
                            var argValue = (bool)namedArgument.Value.Value; // isBatched only takes in booleans and the generator will parse it as a bool so we can type cast this to use in the next line

                            attrProperties["cardinality"] = argValue ? "Many" : "One";
                        }
                        else
                        {
                            if (TryParseValueByType(namedArgument.Value, out object? argValue))
                            {
                                attrProperties[namedArgument.Key] = argValue;
                            }
                            else
                            {
                                _context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.InvalidBindingAttributeArgument, attribLocation));
                                return false;
                            }
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

            private static bool IsArrayOrNotNull(TypedConstant? namedArgument)
            {
                if (namedArgument is null)
                {
                    return false;
                }

                // special handling required for array types which store values differently (cannot check namedArgument.Value.Value)
                if (namedArgument.Value.Kind is TypedConstantKind.Array)
                {
                    return true;
                }
                else
                {
                    return namedArgument.Value.Value != null; // similar to legacy generator, arguments with null values are not written to function metadata
                }
            }

            private bool TryLoadConstructorArguments(AttributeData attributeData, IDictionary<string, object?> arguments, Location? attributeLocation)
            {
                IMethodSymbol? attribMethodSymbol = attributeData.AttributeConstructor;

                // Check if the attribute constructor has any parameters
                if (attribMethodSymbol is null)
                {
                    _context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.SymbolNotFound, attributeLocation, nameof(attribMethodSymbol)));
                    return false;
                }

                // It's fair to assume that constructor arguments appear before named arguments, and
                // that the constructor names would match the property names
                for (int i = 0; i < attributeData.ConstructorArguments.Length; i++)
                {
                    string argumentName = attribMethodSymbol.Parameters[i].Name;
                    OverrideBindingName(attributeData.AttributeClass!, ref argumentName); // either argumentName will remain unchanged OR be updated to the overridden name at the end of this.

                    var arg = attributeData.ConstructorArguments[i];

                    if (TryParseValueByType(arg, out object? argValue))
                    {
                        arguments[argumentName] = argValue;
                    }
                    else
                    {
                        _context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.InvalidBindingAttributeArgument, attributeLocation));
                        return false;
                    }
                }

                return true;
            }

            private bool TryParseValueByType(TypedConstant attributeArg, out object? argValue)
            {
                argValue = null;

                switch (attributeArg.Kind)
                {
                    case TypedConstantKind.Primitive:
                        argValue = attributeArg.Value;
                        break;

                    case TypedConstantKind.Enum:
                        var enumValue = attributeArg.Type!.GetMembers()
                            .FirstOrDefault(m => m is IFieldSymbol field
                                && field.ConstantValue is object value
                                && value.Equals(attributeArg.Value));

                        if (enumValue is null)
                        {
                            return false;
                        }

                        // we want just the enumValue symbol's name (ex: Admin, Anonymous, Function)
                        argValue = enumValue.Name;
                        break;

                    case TypedConstantKind.Array:
                        var arrayValues = attributeArg.Values.Select(a => a.Value?.ToString()).ToArray();
                        argValue = arrayValues;
                        break;
                }

                return argValue is not null;
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
