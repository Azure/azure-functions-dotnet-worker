// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Reflection;
using System.Runtime.InteropServices;

namespace Azure.Functions.Sdk;

/// <summary>
/// Extensions for reflection metadata types read via <see cref="System.Reflection.MetadataLoadContext"/>.
/// </summary>
public static class ReflectionExtensions
{
    extension(CustomAttributeData attribute)
    {
        /// <summary>
        /// Gets the first and second constructor arguments from a custom attribute.
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

            first = (TFirst)attribute.ConstructorArguments[0].Value!;
            second = (TSecond)attribute.ConstructorArguments[1].Value!;
        }
    }

    extension(Type type)
    {
        /// <summary>
        /// Walks the inheritance chain of the type and checks if any type in the chain matches the provided check.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <param name="check">The predicate to check with.</param>
        /// <returns>
        /// <c>true</c> if <paramref name="check"/> succeeds on any type in the inheritance chain, <c>false</c> otherwise.
        /// </returns>
        public bool CheckTypeInheritance(Func<Type, bool> check)
        {
            Throw.IfNull(type);
            Throw.IfNull(check);

            Type? currentType = type;
            while (currentType != null)
            {
                if (check(currentType))
                {
                    return true;
                }

                currentType = currentType.BaseType;
            }

            return false;
        }

        /// <summary>
        /// Gets the full name of the type suitable for reflection use.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The formatted name of the type.</returns>
        public string GetReflectionFullName()
        {
            Throw.IfNull(type);

            // Type.FullName already uses '+' as the nested-type separator, matching reflection semantics.
            return type.FullName ?? type.Name;
        }
    }
}
