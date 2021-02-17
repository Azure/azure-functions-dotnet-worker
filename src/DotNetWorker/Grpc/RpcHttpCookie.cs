// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Microsoft.Azure.WebJobs.Script.Grpc.Messages
{
    public sealed partial class RpcHttpCookie : IHttpCookie
    {
        string IHttpCookie.Domain => Domain.Value;

        DateTimeOffset? IHttpCookie.Expires => Expires?.Value?.ToDateTimeOffset();

        bool? IHttpCookie.HttpOnly => HttpOnly?.Value;

        double? IHttpCookie.MaxAge => MaxAge?.Value;

        string? IHttpCookie.Path => Path?.Value;

        SameSite IHttpCookie.SameSite => (SameSite)SameSite;

        bool? IHttpCookie.Secure => Secure?.Value;

        public RpcHttpCookie(IHttpCookie other) : this()
        {
            name_ = other.Name;
            value_ = other.Value;
            domain_ = other.Domain != null ? new NullableString() { Value = other.Domain } : null;
            path_ = other.Path != null ? new NullableString() { Value = other.Path } : null;
            expires_ = other.Expires != null ? new NullableTimestamp { Value = new Google.Protobuf.WellKnownTypes.Timestamp { Seconds = other.Expires.Value.ToUnixTimeSeconds() } } : null;
            secure_ = other.Secure != null ? new NullableBool { Value = other.Secure.Value } : null;
            httpOnly_ = other.HttpOnly != null ? new NullableBool { Value = other.HttpOnly.Value } : null;
            sameSite_ = (Types.SameSite)other.SameSite;
            maxAge_ = other.MaxAge != null ? new NullableDouble { Value = other.MaxAge.Value } : null;
        }
    }
}
