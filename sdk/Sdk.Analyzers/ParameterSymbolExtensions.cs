// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.Azure.Functions.Worker.Sdk.Analyzers
{
    internal static class ParameterSymbolExtensions
    {
        public static AttributeData GetWebJobsAttribute(this IParameterSymbol parameter)
        {
            var parameterAttributes = parameter.GetAttributes();

            foreach (var parameterAttribute in parameterAttributes)
            {
                var attributeAttributes = parameterAttribute.AttributeClass.GetAttributes();

                foreach (var attribute in attributeAttributes)
                {
                    if (string.Equals(attribute.AttributeClass.ToDisplayString(), Constants.Types.WebJobsBindingAttribute, StringComparison.Ordinal))
                    {
                        return parameterAttribute;
                    }
                }
            }

            return null;
        }
    }
}
