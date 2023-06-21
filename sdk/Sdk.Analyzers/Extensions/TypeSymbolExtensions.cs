// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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

        internal static List<AttributeData> GetInputConverterAttributes(this ITypeSymbol attributeType, Compilation compilation)
        {
            var inputConverterAttributeType = compilation.GetTypeByMetadataName(Constants.Types.InputConverterAttribute);
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

        internal static bool IsIterableType(this ITypeSymbol typeSymbol, SymbolAnalysisContext context)
        {
            bool isArrayType = false;

            if (typeSymbol is IArrayTypeSymbol)
            {
                if (string.Equals(typeSymbol.ToString(), typeof(byte[]).Name, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                isArrayType = true;
            }

            bool IsIEnumerableTType = typeSymbol.IsOrImplementsOrDerivesFrom(context.Compilation.GetTypeByMetadataName(typeof(IEnumerable<>).FullName)!);

            var IsIEnumerableType = typeSymbol.IsOrImplementsOrDerivesFrom(context.Compilation.GetTypeByMetadataName(typeof(IEnumerable).FullName)!);

            return IsIEnumerableTType || IsIEnumerableType || isArrayType;
        }

        internal static bool IsOrImplementsOrDerivesFrom(this ITypeSymbol symbol, ITypeSymbol? other)
        {
            return symbol.IsOrImplements(other) || symbol.IsOrDerivedFrom(other);
        }

        internal static bool IsOrDerivedFrom(this ITypeSymbol symbol, ITypeSymbol? other)
        {
            if (other is null)
            {
                return false;
            }

            var current = symbol;

            while (current != null)
            {
                if (SymbolEqualityComparer.Default.Equals(current, other) || SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, other))
                {
                    return true;
                }

                current = current.BaseType;
            }

            return false;
        }

        internal static bool IsOrImplements(this ITypeSymbol symbol, ITypeSymbol? other)
        {
            if (other is null)
            {
                return false;
            }

            if (symbol.Name == typeof(string).Name)
            {
                return false;
            }

            var current = symbol;

            while (current != null)
            {
                foreach (var member in current.Interfaces)
                {
                    if (IsOrDerivedFrom(member, other))
                    {
                        return true;
                    }
                }

                current = current.BaseType;
            }

            return false;
        }
    }
}
