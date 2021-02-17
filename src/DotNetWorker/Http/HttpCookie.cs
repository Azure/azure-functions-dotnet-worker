// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Http
{
    public sealed class HttpCookie : IHttpCookie
    {
        public HttpCookie(string name, string value)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        public string? Domain { get; set; }

        public DateTimeOffset? Expires { get; set; }

        public bool? HttpOnly { get; set; }

        public double? MaxAge { get; set; }

        public string Name { get; set; }

        public string? Path { get; set; }

        public SameSite SameSite { get; set; }

        public bool? Secure { get; set; }

        public string Value { get; set; }
    }
}
