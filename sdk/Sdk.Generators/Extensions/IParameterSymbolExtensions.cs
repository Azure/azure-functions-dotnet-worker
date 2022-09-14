using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Microsoft.Azure.Functions.Worker.Sdk.Generators.Extensions
{
    internal static class IParameterSymbolExtensions
    {
        internal static bool IsOrDerivedFrom(this IParameterSymbol parameterSymbol, string typeFullName)
        {
            var baseType = parameterSymbol.Type;

            while (baseType != null)
            {
                if (String.Equals(baseType.GetFullName(), typeFullName, StringComparison.Ordinal))
                {
                    return true;
                }
                baseType = baseType.BaseType;
            }

            return false;
        }
    }
}
