// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

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
        /// <param name="symbol"></param>
        /// <returns></returns>
        internal static string GetFullName(this ITypeSymbol typeSymbol)
        {
            var symbol = typeSymbol as ISymbol;

            if (symbol == null || IsRootNamespace(symbol))
            {
                return string.Empty;
            }

            var sb = new StringBuilder(symbol.MetadataName);
            symbol = symbol.ContainingSymbol;

            while (!IsRootNamespace(symbol))
            {
                sb.Insert(0, '.');
                sb.Insert(0, symbol.MetadataName);
                symbol = symbol.ContainingSymbol;
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
    }
}
