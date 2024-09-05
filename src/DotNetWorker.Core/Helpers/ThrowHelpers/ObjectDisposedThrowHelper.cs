// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker
{
    internal static class ObjectDisposedThrowHelper
    {
        /// <summary>Throws an <see cref="ObjectDisposedException"/> if the specified <paramref name="condition"/> is <see langword="true"/>.</summary>
        /// <param name="condition">The condition to evaluate.</param>
        /// <param name="instance">The object whose type's full name should be included in any resulting <see cref="ObjectDisposedException"/>.</param>
        /// <exception cref="ObjectDisposedException">The <paramref name="condition"/> is <see langword="true"/>.</exception>
        internal static void ThrowIf(bool condition, object instance)
        {
#if NET8_0_OR_GREATER
            ObjectDisposedException.ThrowIf(condition, instance);
#else
            if (condition)
            {
                throw new ObjectDisposedException(instance?.GetType().FullName);
            }
#endif
        }

    }
}
