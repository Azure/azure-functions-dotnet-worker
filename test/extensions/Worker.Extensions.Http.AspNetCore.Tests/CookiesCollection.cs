// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;

namespace Worker.Extensions.Http.AspNetCore.Tests
{
    internal class CookiesCollection : IRequestCookieCollection, IDictionary<string, string>
    {
        private readonly Dictionary<string, string> _cookies = new();

        public string? this[string key] => _cookies[key];

        string IDictionary<string, string>.this[string key]
        {
            get => _cookies[key];
            set => _cookies[key] = value;
        }

        public int Count => _cookies.Count;

        public ICollection<string> Keys => _cookies.Keys;

        public ICollection<string> Values => _cookies.Values;

        public bool IsReadOnly => ((ICollection<KeyValuePair<string, string>>)_cookies).IsReadOnly;

        public void Add(string key, string value) => _cookies.Add(key, value);

        public void Add(KeyValuePair<string, string> item) => _cookies.Add(item.Key, item.Value);

        public void Clear()=> _cookies.Clear();

        public bool Contains(KeyValuePair<string, string> item) => _cookies.Contains(item);

        public bool ContainsKey(string key) => _cookies.ContainsKey(key);

        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<string, string>>)_cookies).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
            => _cookies.GetEnumerator();

        public bool Remove(string key)
        {
            return ((IDictionary<string, string>)_cookies).Remove(key);
        }

        public bool Remove(KeyValuePair<string, string> item) => _cookies.Remove(item.Key);

        public bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
            => _cookies.TryGetValue(key, out value);

        IEnumerator IEnumerable.GetEnumerator()
            => _cookies.GetEnumerator();
    }
}
