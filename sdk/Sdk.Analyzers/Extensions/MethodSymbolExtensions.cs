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
        /// <summary>
        /// Gets a collection of web jobs attributes present on the method symbol.
        /// This includes attributes from parameters and return type.
        /// </summary>
        /// <param name="symbol">The method symbol to check.</param>
        /// <returns>Enumerable collection of web jobs attributes present on the method symbol.</returns>
        public static IEnumerable<AttributeData> GetWebJobsAttributes(this IMethodSymbol symbol)
        {
            var webJobsAttributesFromParams = symbol.Parameters.Select(p => p.GetWebJobsAttribute()).Where(a => a is not null);
            var webJobsAttributesFromReturnType = symbol.GetReturnTypeAttributes().Where(a => a.IsWebJobAttribute());

            return webJobsAttributesFromParams.Concat(webJobsAttributesFromReturnType);
        }

        /// <summary>
        /// Checks if a method symbol is an azure function in the isolated model.
        /// </summary>
        /// <param name="symbol">The method symbol to check.</param>
        /// <param name="analysisContext">The context for the symbol analysis.</param>
        /// <returns>A boolean value indicating whether the method symbol is an azure function.</returns>
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
