// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace System.Collections.Immutable;

/// <summary>
/// Contains extension methods for collections.
/// </summary>
internal static class ImmutableCollectionExtensions
{
    extension(ImmutableDictionary)
    {
        /// <summary>
        /// Creates an immutable dictionary from a span of key/value pairs.
        /// </summary>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <typeparam name="TValue">The value type.</typeparam>
        /// <param name="items">The items to create with.</param>
        /// <returns>An immutable dictionary with items added.</returns>
        public static ImmutableDictionary<TKey, TValue> CreateRange<TKey, TValue>(
            ReadOnlySpan<KeyValuePair<TKey, TValue>> items)
            where TKey : notnull
        {
            if (items.Length == 0)
            {
                return ImmutableDictionary<TKey, TValue>.Empty;
            }

            if (items.Length == 1)
            {
                KeyValuePair<TKey, TValue> item = items[0];
                return ImmutableDictionary<TKey, TValue>.Empty.Add(item.Key, item.Value);
            }

            ImmutableDictionary<TKey, TValue>.Builder builder = ImmutableDictionary.CreateBuilder<TKey, TValue>();
            foreach (KeyValuePair<TKey, TValue> item in items)
            {
                builder.Add(item);
            }

            return builder.ToImmutable();
        }
    }

    extension<TKey, TValue> (ImmutableDictionary<TKey, TValue> dict)
        where TKey : notnull
    {
        /// <summary>
        /// Sets all items from the span on a new immutable dictionary.
        /// </summary>
        /// <param name="items">The items to set on the dictionary.</param>
        /// <returns>An immutable dictionary with items set.</returns>
        public ImmutableDictionary<TKey, TValue> SetItems(ReadOnlySpan<KeyValuePair<TKey, TValue>> items)
        {
            if (items.Length == 0)
            {
                return dict;
            }

            if (items.Length == 1)
            {
                KeyValuePair<TKey, TValue> item = items[0];
                return dict.SetItem(item.Key, item.Value);
            }

            ImmutableDictionary<TKey, TValue>.Builder builder = dict.ToBuilder();
            foreach (KeyValuePair<TKey, TValue> item in items)
            {
                builder[item.Key] = item.Value;
            }

            return builder.ToImmutable();
        }
    }
}
