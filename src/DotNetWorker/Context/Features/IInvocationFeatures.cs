// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// Represents a collection of invocation features.
    /// </summary>
    public interface IInvocationFeatures : IEnumerable<KeyValuePair<Type, object>>
    {
        /// <summary>
        /// Sets a feature of the provided type.
        /// </summary>
        /// <typeparam name="T">The feature Type.</typeparam>
        /// <param name="instance">The instance of the feature.</param>
        void Set<T>(T instance);

        /// <summary>
        /// Gets a feature with the specified type for the current invocation.
        /// </summary>
        /// <typeparam name="T">The feature Type.</typeparam>
        /// <returns>The feature instance, or null.</returns>
        T? Get<T>();
    }
}
