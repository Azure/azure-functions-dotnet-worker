using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Microsoft.Azure.Functions.Worker.Sdk.FunctionProvider.SourceBuilder
{
    internal sealed class BindingMetadataSourceBuilder : ISourceBuilder
    {
        // TODO: Maybe move this to so injectable class for ease of testing
        private static int _arrayCount = 0;

        private readonly ParameterSyntax _parameterSyntax;
        private readonly SemanticModel _semanticModel;
        private readonly IParameterSymbol _parameterSymbol;
        private readonly AttributeData _bindingAttrib;
        private readonly string _functionName;

        private StringBuilder? _sourceStringBuilder;

        public BindingMetadataSourceBuilder(ParameterSyntax parameterSyntax, SemanticModel semanticModel, string functionName)
        {
            _parameterSyntax = parameterSyntax ?? throw new ArgumentNullException(nameof(parameterSyntax));
            _semanticModel = semanticModel ?? throw new ArgumentNullException(nameof(semanticModel));
            _functionName = functionName ?? throw new ArgumentNullException(nameof(functionName));

            _parameterSymbol = _semanticModel.GetDeclaredSymbol(_parameterSyntax) as IParameterSymbol
                ?? throw new InvalidOperationException("Cannot find parameter symbol from parameter syntax.");
            _bindingAttrib = _parameterSymbol.GetAttributes().First();
        }

        public string Build()
        {
            _sourceStringBuilder = new StringBuilder();

            AddBinding();

            return _sourceStringBuilder.ToString();
        }

        private void AddBinding()
        {
            // create binding metadata w/ info below and add to function metadata created above
            string attributeName = _bindingAttrib.AttributeClass!.Name;
            string triggerName = _parameterSyntax.Identifier.ValueText; // correct?
            string triggerType = attributeName.Replace("Attribute", "");
            string bindingDirection = GetBindingDirection(_parameterSymbol);

            AddRequiredBindingProperties(triggerName, triggerType, bindingDirection);
            AddBindingSpecificProperties();
            AddBindingMetadataToFunction();

            AddHttpOutputBindingIfHttpTrigger(triggerType);
        }

        private void AddRequiredBindingProperties(string triggerName, string triggerType, string bindingDirection)
        {
            // TODO: Make raw customizable
            // create raw JObject for the BindingMetadata
            _sourceStringBuilder!.Append($@"
                raw = new JObject();
                raw[""name""] = ""{triggerName}"";
                raw[""direction""] = ""{bindingDirection}"";
                raw[""type""] = ""{triggerType}"";
            ");
        }

        private void AddBindingSpecificProperties()
        {
            IDictionary<string, object?> attributeProperties = AttributeDataHelpers.GetAllProperties(_bindingAttrib);

            foreach (KeyValuePair<string, object?> prop in attributeProperties)
            {
                AddBindingSpecificProperty(prop.Key, prop.Value);
            }
        }

        private void AddHttpOutputBindingIfHttpTrigger(string triggerType)
        {
            if (string.Equals(triggerType, "httptrigger", StringComparison.OrdinalIgnoreCase))
            {
                _sourceStringBuilder!.Append(@"
                    raw = new JObject();
                    raw[""name""] = ""$return"";
                    raw[""direction""] = ""out"";
                    raw[""type""] = ""http"";
                ");

                AddBindingMetadataToFunction();
            }
        }

        private void AddBindingMetadataToFunction()
        {
            _sourceStringBuilder!.Append(_functionName + @".Bindings.Add(BindingMetadata.Create(raw));");
        }

        private void AddBindingSpecificProperty(string propertyName, object? propertyValue)
        {
            if (propertyValue?.GetType().IsArray ?? false)
            {
                string jarr = GetArrayInitializationCode((propertyValue as IEnumerable)!);

                _sourceStringBuilder!.Append(@$"
                    {jarr}
                    raw[""{propertyName}""] = jarr{_arrayCount++};
                ");
            }
            else
            {
                var formattedValue = ToJsonValue(propertyValue);
                _sourceStringBuilder!.Append(@$"
                    raw[""{propertyName}""] = {formattedValue};
                ");
            }
        }

        internal static string GetBindingDirection(IParameterSymbol parameterSymbol)
        {
            string bindingDirection = "in";

            if (parameterSymbol.Type is INamedTypeSymbol parameterNamedType &&
                parameterNamedType.IsGenericType &&
                parameterNamedType.ConstructUnboundGenericType().ToString() == "Microsoft.Azure.Functions.Worker.OutputBinding<>")
            {
                bindingDirection = "out";
            }

            return bindingDirection;
        }

        private static string ToJsonValue(object? propValue)
        {
            if (propValue != null)
            {
                return @$"""{propValue}""";
            }
            else
            {
                return "null";
            }
        }

        internal static string GetArrayInitializationCode(IEnumerable enumerableValues)
        {
            Type propType = enumerableValues.GetType();
            Type elementType = propType.GetElementType();

            IEnumerable<object> objectValues = enumerableValues.Cast<object>();
            string allItems = string.Join(", ", objectValues.Select(el => ToJsonValue(el)));

            return @$"
                var arr{_arrayCount} = new {elementType}[] {{ {allItems} }};
                var jarr{_arrayCount} = new JArray(arr{_arrayCount});
            ";
        }
    }
}
