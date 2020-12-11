using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Azure.Functions.Worker.Sdk.FunctionProvider.SourceBuilder
{
    public static class AttributeDataHelpers
    {
        public static IDictionary<string, object?> GetAllProperties(AttributeData attributeData)
        {
            Dictionary<string, object?> argumentData = new();
            if (attributeData.ConstructorArguments.Any())
            {
                LoadConstructorArguments(attributeData, argumentData);
            }

            foreach (var namedArgument in attributeData.NamedArguments)
            {
                if (namedArgument.Value.Value != null)
                {
                    argumentData[namedArgument.Key] = namedArgument.Value.Value;
                }
            }

            return argumentData;
        }

        private static void LoadConstructorArguments(AttributeData attributeData, IDictionary<string, object?> dict)
        {
            IMethodSymbol? attributeConstructor = attributeData.AttributeConstructor;

            if (attributeConstructor is null)
            {
                return;
            }

            // It's fair to assume than constructor arguments appear before named arguments, and
            // that the constructor names would match the property names
            for (int i = 0; i < attributeData.ConstructorArguments.Length; i++)
            {
                var argumentName = attributeConstructor.Parameters[i].Name;

                var arg = attributeData.ConstructorArguments[i];
                switch (arg.Kind)
                {
                    case TypedConstantKind.Error:
                        break;
                    case TypedConstantKind.Primitive:
                    case TypedConstantKind.Enum:
                        dict[argumentName] = arg.Value;
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
        }
    }
}
