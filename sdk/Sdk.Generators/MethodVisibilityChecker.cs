// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Microsoft.Azure.Functions.Worker.Sdk.Generators
{
    internal static class MethodVisibilityChecker
    {
        /// <summary>
        /// Determines the visibility of a method.
        /// The visibility of a method is determined by the following rules:
        ///     1. If the method is public, and all containing types are public, return PublicAndVisible
        ///     2. If the method is public, but one or more containing types are not public, return PublicButContainingTypeNotVisible
        ///     3. If the method is not public, return NotPublic
        /// </summary>
        /// <param name="methodSymbol"></param>
        /// <returns></returns>
        internal static FunctionMethodVisibility GetVisibility(IMethodSymbol methodSymbol)
        {
            // Check if the symbol itself is public
            if (methodSymbol.DeclaredAccessibility == Accessibility.Public)
            {
                // Check if any containing type is not public
                INamedTypeSymbol containingType = methodSymbol.ContainingType;
                while (containingType != null)
                {
                    if (containingType.DeclaredAccessibility != Accessibility.Public)
                    {
                        return FunctionMethodVisibility.PublicButContainingTypeNotVisible;
                    }
                    containingType = containingType.ContainingType;
                }

                // If both the symbol and all containing types are public, return PublicAndVisible
                return FunctionMethodVisibility.PublicAndVisible;
            }
            else
            {
                // If the symbol itself is not public, return NotPublic
                return FunctionMethodVisibility.NotPublic;
            }
        }
    }
}
