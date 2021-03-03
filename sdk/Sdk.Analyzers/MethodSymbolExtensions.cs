// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.Azure.Functions.Worker.Sdk.Analyzers
{
    internal static class MethodSymbolExtensions
    {
        public static IEnumerable<AttributeData> GetWebJobsAttributes(this IMethodSymbol symbol)
        {
            return symbol.Parameters.Select(p => p.GetWebJobsAttribute()).Where(a => a is not null);
        }

        public static bool IsFunction(this IMethodSymbol symbol, SymbolAnalysisContext analysisContext)
        {
            var attributes = symbol.GetAttributes();

            if (attributes.IsEmpty)
            {
                return false;
            }

            var attributeType = analysisContext.Compilation.GetTypeByMetadataName(Constants.Types.WorkerFunctionAttribute);

            return attributes.Any(a => attributeType.IsAssignableFrom(a.AttributeClass, true));
        }
    }
}
