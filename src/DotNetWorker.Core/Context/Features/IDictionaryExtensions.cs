// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// This is excluded in the project for other targets,
// but conditionally compiling for clarity.
// This implementation will be used in .NET Standard 2.0
#if NETSTANDARD2_0
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;

internal static class IDictionaryExtensions
{
    internal static bool TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (dictionary.ContainsKey(key))
        {
            return false;
        }

        dictionary.Add(key, value);
        return true;
    }
}

internal static class ConcurrentDictionaryExtensions
{
    public static TValue GetOrAdd<TKey, TValue, TArg>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TArg, TValue> valueFactory, TArg factoryArgument)
    {
        if (dictionary == null)
        {
            throw new ArgumentNullException(nameof(dictionary));
        }

        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (valueFactory == null)
        {
            throw new ArgumentNullException(nameof(valueFactory));
        }

        return dictionary.GetOrAdd(key, k => valueFactory(k, factoryArgument));
    }
}

internal static class KeyValuePairExtensions
{
    // Based on https://source.dot.net/#System.Private.CoreLib/src/libraries/System.Private.CoreLib/src/System/Collections/Generic/KeyValuePair.cs,aa57b8e336bf7f59    
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> pair, out TKey key, out TValue value)
    {
        key = pair.Key;
        value = pair.Value;
    }
}

internal static class IReadOnlyDictionaryExtensions
{
    internal static TValue GetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        return dictionary.TryGetValue(key, out TValue? value) ? value : defaultValue;
    }
}

#endif
