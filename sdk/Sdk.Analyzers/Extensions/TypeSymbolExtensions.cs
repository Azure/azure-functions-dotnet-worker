// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.Azure.Functions.Worker.Sdk.Analyzers
{
    internal static class TypeSymbolExtensions
    {
        internal static bool IsAssignableFrom(this ITypeSymbol targetType, ITypeSymbol sourceType, bool exactMatch = false)
        {
            if (targetType != null)
            {
                while (sourceType != null)
                {
                    if (sourceType.Equals(targetType, SymbolEqualityComparer.Default))
                    {
                        return true;
                    }

                    if (exactMatch)
                    {
                        return false;
                    }

                    if (targetType.TypeKind == TypeKind.Interface)
                    {
                        return sourceType.AllInterfaces.Any(i => i.Equals(targetType, SymbolEqualityComparer.Default));
                    }

                    sourceType = sourceType.BaseType;
                }
            }

            return false;
        }

        internal static List<AttributeData> GetInputConverterAttributes(this ITypeSymbol attributeType, SymbolAnalysisContext context)
        {
            var inputConverterAttributeType = context.Compilation.GetTypeByMetadataName(Constants.Types.InputConverterAttribute);
            return attributeType.GetAttributes()
                .Where(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, inputConverterAttributeType))
                .ToList();
        }

        internal static List<AttributeData> GetInputConverterAttributes(this ITypeSymbol attributeType, SemanticModel context)
        {
            var inputConverterAttributeType = context.Compilation.GetTypeByMetadataName(Constants.Types.InputConverterAttribute);
            return attributeType.GetAttributes()
                .Where(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, inputConverterAttributeType))
                .ToList();
        }

        internal static string GetMinimalDisplayName(this ITypeSymbol type, SemanticModel semanticModel)
        {
            string name = type.ToMinimalDisplayString(semanticModel, 0);

            if (name.Contains("IEnumerable"))
            {
                name = Regex.Match(name, @"IEnumerable<[^>]+>").Value;
            }

            return name;
        }
    }
}
