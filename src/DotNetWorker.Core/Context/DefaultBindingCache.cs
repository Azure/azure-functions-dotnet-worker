// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// In memory cache for binding data (per invocation)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class DefaultBindingCache<T> : IBindingCache<T>
    {
        private readonly ConcurrentDictionary<string, T> _cache = new();

        public bool TryGetValue(string key, out T? value)
        {
            EnsureKeyNotNull(key);

            return _cache.TryGetValue(key, out value);
        }

        public bool TryAdd(string key, T value)
        {
            EnsureKeyNotNull(key);

            return _cache.TryAdd(key, value);
        }

        private static void EnsureKeyNotNull(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }
        }
    }
}
