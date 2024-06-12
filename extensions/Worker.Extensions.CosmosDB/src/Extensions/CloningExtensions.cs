// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.Azure.Cosmos;

namespace Microsoft.Azure.Functions.Worker
{
    internal static class CloningExtensions
    {
        public static T MemberwiseClone<T>(this T original)
        {
            if (original is null)
            {
                throw new ArgumentNullException(nameof(original));
            }

            // Get the type of the object
            Type type = original.GetType();

            // Get the internal Clone method
            MethodInfo cloneMethod = type.GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);

            if (cloneMethod is null)
            {
                throw new InvalidOperationException("The MemberwiseClone method could not be found.");
            }

            // Invoke the Clone method
            return (T)cloneMethod.Invoke(original, null);
        }
    }
}
