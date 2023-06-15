// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;

namespace Microsoft.Azure.Functions.Worker.Sdk.Analyzers
{
    internal static class NamedSymbolExtensions
    {
        internal static bool IsInputOrTriggerBinding(this INamedTypeSymbol symbol)
        {
            var baseType = symbol.BaseType?.ToDisplayString();

            if (string.Equals(baseType,Constants.Types.InputBindingAttribute, StringComparison.Ordinal)
                || string.Equals(baseType,Constants.Types.TriggerBindingAttribute, StringComparison.Ordinal))
            {
                return true;
            }

            return false;
        }
    }
}
