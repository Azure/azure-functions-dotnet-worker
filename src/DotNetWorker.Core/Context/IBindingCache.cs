// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker
{
    internal interface IBindingCache<T>
    {
        /// <summary>
        /// Attempts to get the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">The value of the cache entry for the requested key.</param>
        /// <returns>true if the key was found in the cache, else false.</returns>
        bool TryGetValue(string key, out T? value);

        /// <summary>
        /// Attempts to add the specified key and value to the cache.
        /// </summary>
        /// <param name="key">The key of the cache entry.</param>
        /// <param name="value">The value of the cache entry.</param>
        /// <returns>true if the key/value pair was added to the cache, else false.</returns>
        bool TryAdd(string key, T value);
    }
}
