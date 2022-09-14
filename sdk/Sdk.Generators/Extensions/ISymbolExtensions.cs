// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Microsoft.Azure.Functions.Worker.Sdk.Generators
{
    internal static class ISymbolExtensions
    {
        /// <summary>
        /// Walks the symbol tree to generate the fully qualified name of a type symbol.
        /// Ex input: A Symbol for "Task" token
        /// Output: "System.Threading.Tasks.Task"
        /// </summary>
        /// <param name="typeSymbol"></param>
        /// <returns></returns>
        internal static string GetFullName(this ITypeSymbol typeSymbol)
        {
            var symbol = typeSymbol as ISymbol;

            if (symbol == null || IsRootNamespace(symbol))
            {
                return string.Empty;
            }

            var sb = new StringBuilder(symbol.MetadataName);

            if (symbol is IArrayTypeSymbol arraySymbol) // arrays need to be handled differently b/c the properties used to get the full name for other symbols are null for IArrayTypeSymbols
            {
                sb.Append(arraySymbol.ElementType.GetFullName()); // ex: for string[], the ElementType is System.String and that is the full name returned at this step.
                sb.Append("[]"); // System.Byte[], System.String[] are the full names for array types of element type Byte, String and we auto-add the brackets here.
            }
            else
            {
                symbol = symbol.ContainingSymbol;

                while (!IsRootNamespace(symbol))
                {
                    sb.Insert(0, '.');
                    sb.Insert(0, symbol.MetadataName);
                    symbol = symbol.ContainingSymbol;
                }
            }

            return sb.ToString();
        }

        private static bool IsRootNamespace(ISymbol symbol)
        {
            if (symbol is INamespaceSymbol namespaceSymbol)
            {
                return namespaceSymbol.IsGlobalNamespace;
            }

            return false;
        }

        internal static string? GetDataTypeFromType(this ITypeSymbol symbol)
        {
            string? dataType = null;

            if (symbol.IsStringType())
            {
                dataType = "\"String\"";
            }
            // Is binary parameter type
            else if (symbol.IsBinaryType())
            {
                dataType = "\"Binary\"";
            }

            return dataType;
        }

        internal static bool IsStringType(this ITypeSymbol symbol)
        {
            var fullName = symbol.GetFullName();
            return String.Equals(fullName, Constants.StringType, StringComparison.Ordinal);
        }

        internal static bool IsBinaryType(this ITypeSymbol symbol)
        {
            var fullName = symbol.GetFullName();
            return String.Equals(fullName, Constants.ByteArrayType, StringComparison.Ordinal)
                || String.Equals(fullName, Constants.ReadOnlyMemoryOfBytes, StringComparison.Ordinal);
        }
    }
}
