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
}
