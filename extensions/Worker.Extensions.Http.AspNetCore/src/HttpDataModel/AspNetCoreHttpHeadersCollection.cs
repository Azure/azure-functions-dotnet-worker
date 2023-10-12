// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using HeadersEnumerable = System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>>;

namespace Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore
{
    internal sealed class AspNetCoreHttpRequestHeadersCollection : AspNetCoreHttpHeadersCollection
    {
        public AspNetCoreHttpRequestHeadersCollection(HttpRequest request)
            : base(request.Headers)
        {
            var requestFeature = request.HttpContext.Features.Get<IHttpRequestFeature>()!;
            requestFeature.Headers = this;
        }
    }

    internal sealed class AspNetCoreHttpResponseHeadersCollection : AspNetCoreHttpHeadersCollection
    {
        private IHeaderDictionary _originalResponseHeaders;
        public AspNetCoreHttpResponseHeadersCollection(HttpResponse request)
            : base(request.Headers)
        {
            var requestFeature = request.HttpContext.Features.Get<IHttpResponseFeature>()!;
            _originalResponseHeaders = requestFeature.Headers;
            requestFeature.Headers = this;

            // Even though we're replacing the response feature headers, we need to make sure
            // headers are written back to the original reference before sent to the client as
            // that reference is still used.
            request.OnStarting(ProcessResponseStarting);
        }

        public AspNetCoreHttpResponseHeadersCollection(HttpResponse request, HttpHeadersCollection headers)
            : base(headers)
        {
            var requestFeature = request.HttpContext.Features.Get<IHttpResponseFeature>()!;
            _originalResponseHeaders = requestFeature.Headers;
            requestFeature.Headers = this;
        }

        private Task ProcessResponseStarting()
        {
            _originalResponseHeaders.Clear();
            foreach (var item in ((HeadersEnumerable)this))
            {
                _originalResponseHeaders.Add(item.Key, item.Value);
            }

            return Task.CompletedTask;
        }
    }

    internal abstract class AspNetCoreHttpHeadersCollection : HttpHeadersCollection, IHeaderDictionary
    {
        public AspNetCoreHttpHeadersCollection(IHeaderDictionary headers)
        {
            foreach ( var header in headers)
            {
                TryAddWithoutValidation(header.Key, (IEnumerable<string>)header.Value);
            }
        }

        public AspNetCoreHttpHeadersCollection(HttpHeadersCollection headers)
        {
            foreach (var header in headers)
            {
                TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        StringValues IHeaderDictionary.this[string key]
        {
            get
            {
                if (TryGetValues(key, out var value))
                {
                    return new StringValues(value.ToArray());
                }

                return StringValues.Empty;
            }
            set
            {
                TryAddWithoutValidation(key, (IEnumerable<string>)value);
            }
        }

        StringValues IDictionary<string, StringValues>.this[string key] { get => ((IHeaderDictionary)this)[key]; set => ((IHeaderDictionary)this)[key] = value; }

        public long? ContentLength
        {
            get
            {
                var headerValue = ((IHeaderDictionary)this)[HeaderNames.ContentLength];
                if (headerValue.Count == 1 &&
                    !string.IsNullOrEmpty(headerValue[0]) &&
                    HeaderUtilities.TryParseNonNegativeInt64(new StringSegment(headerValue[0]).Trim(), out long value))
                {
                    return value;
                }

                return null;
            }
            set
            {
                if (value.HasValue)
                {
                    ((IHeaderDictionary)this)[HeaderNames.ContentLength] = HeaderUtilities.FormatNonNegativeInt64(value.GetValueOrDefault());
                }
                else
                {
                    Remove(HeaderNames.ContentLength);
                }
            }
        }


        public StringValues this[string key]
        {
            get
            {
                if (TryGetValues(key, out var value))
                {
                    return new StringValues(value.ToArray());
                }

                return StringValues.Empty;
            }
            set
            {
                TryAddWithoutValidation(key, (IEnumerable<string>)value);
            }
        }

        public ICollection<string> Keys => NonValidated.Select(x => x.Key).ToList();

        public ICollection<StringValues> Values => NonValidated.Select(x => new StringValues(x.Value.ToArray())).ToList();

        public int Count => base.NonValidated.Count;

        public bool IsReadOnly => false;

        public void Add(string key, StringValues value)
        {
            _ = TryAddWithoutValidation(key, (IEnumerable<string>)value);
        }

        public void Add(KeyValuePair<string, StringValues> item) => Add(item.Key, item.Value);

        public bool Contains(KeyValuePair<string, StringValues> item)
        {
            if (!TryGetValues(item.Key, out var value)
                || !StringValues.Equals(new StringValues(value!.ToArray()), item.Value))
            {
                return false;
            }
            return true;
        }

        public bool ContainsKey(string key) => base.Contains(key);

        public void CopyTo(KeyValuePair<string, StringValues>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(KeyValuePair<string, StringValues> item)
        {
            if (this.Contains(item))
            {
                return base.Remove(item.Key);
            }

            return false;
        }

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out StringValues value)
        {
            if (TryGetValues(key, out var values))
            {
                value = new StringValues(values.ToArray());
                return true;
            }

            value = default;
            return false;
        }

        protected IEnumerator<KeyValuePair<string, StringValues>> GetHeaders()
        {
            foreach (var item in NonValidated)
            {
                yield return new KeyValuePair<string, StringValues>(item.Key, new StringValues(item.Value.ToArray()));
            }
        }

        IEnumerator<KeyValuePair<string, StringValues>> HeadersEnumerable.GetEnumerator()
            => GetHeaders();
    }
}
