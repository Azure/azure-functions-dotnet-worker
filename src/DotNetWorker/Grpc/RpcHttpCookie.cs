// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using Microsoft.Azure.Functions.Worker;

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
    }
}
