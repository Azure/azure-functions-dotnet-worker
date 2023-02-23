// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Microsoft.Azure.Functions.Worker.Sdk.Analyzers
{
    internal static class ParameterSymbolExtensions
    {
        /// <summary>
        /// Gets the first web jobs Attribute if the parameter symbol has any, else returns null.
        /// </summary>
        /// <param name="parameter">The parameter symbol to check.</param>
        /// <returns>An instance of <see cref="AttributeData"/> for the WebJobs Attribute found if any, else null.</returns>
        public static AttributeData GetWebJobsAttribute(this IParameterSymbol parameter)
        {
            var parameterAttributes = parameter.GetAttributes();

            foreach (var parameterAttribute in parameterAttributes)
            {
                if (parameterAttribute.IsWebJobAttribute())
                {
                    return parameterAttribute;
                }
            }

            return null;
        }
    }
}
