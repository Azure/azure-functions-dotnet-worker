// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore
{
    internal class SymbolUtils
    {
        private static string TaskWrapperTypeName = "System.Threading.Tasks.Task`1";

        internal static bool TryUnwrapTaskOfT(ISymbol symbol, SemanticModel semanticModel, out ITypeSymbol resultSymbol)
        {
            var taskType = semanticModel.Compilation.GetTypeByMetadataName(TaskWrapperTypeName);

            resultSymbol = null;

            if (symbol is INamedTypeSymbol namedTypeSymbol &&
                namedTypeSymbol is not null &&
                namedTypeSymbol.ConstructedFrom is not null &&
                SymbolEqualityComparer.Default.Equals(namedTypeSymbol.ConstructedFrom, taskType) &&
                namedTypeSymbol.TypeArguments.Length == 1)
            {
                resultSymbol = namedTypeSymbol.TypeArguments[0];
                return true;
            }

            return false;
        }
    }
}
