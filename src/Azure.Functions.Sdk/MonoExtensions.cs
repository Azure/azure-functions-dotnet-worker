// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Mono.Cecil;

namespace Azure.Functions.Sdk;

/// <summary>
/// Extensions for Mono.Cecil types.
/// </summary>
public static class MonoExtensions
{
    extension(CustomAttribute attribute)
    {
        /// <summary>
        /// Gets the first and second arguments from a custom attribute.
        /// </summary>
        /// <typeparam name="TFirst">The expected type of the first argument.</typeparam>
        /// <typeparam name="TSecond">The expected type of the second argument.</typeparam>
        /// <param name="first">The value of the first argument.</param>
        /// <param name="second">The value of the second argument.</param>
        public void GetArguments<TFirst, TSecond>(out TFirst first, out TSecond second)
        {
            Throw.IfNull(attribute);
            Throw.IfLessThan(attribute.ConstructorArguments.Count, 2,
                "Expected at least two constructor arguments for the attribute.");

            first = (TFirst)attribute.ConstructorArguments[0].Value;
            second = (TSecond)attribute.ConstructorArguments[1].Value;
        }
    }

    /// <summary>
    /// Walks the inheritance chain of the type and checks if any type in the chain matches the provided check.
    /// </summary>
    /// <param name="type">The type reference to check.</param>
    /// <param name="check">The predicate to check with.</param>
    /// <returns>
    /// <c>true</c> if <paramref name="check"/> succeeds on any type in the inheritance chain, <c>false</c> otherwise.
    /// </returns>
    public static bool CheckTypeInheritance(this TypeReference type, Func<TypeReference, bool> check)
    {
        Throw.IfNull(type);
        Throw.IfNull(check);

        TypeReference? currentType = type;
        while (currentType != null)
        {
            if (check(currentType))
            {
                return true;
            }

            currentType = currentType.Resolve()?.BaseType;
        }

        return false;
    }

    /// <summary>
    /// Gets the full name of the type reference suitable for reflection use.
    /// </summary>
    /// <param name="type">The type reference.</param>
    /// <returns>The formatted name of the type.</returns>
    public static string GetReflectionFullName(this TypeReference type)
    {
        Throw.IfNull(type);
        return type.FullName.Replace("/", "+");
    }
}
