// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Http
{
    /// <summary>
    /// A representation of an HTTP cookie
    /// </summary>
    public interface IHttpCookie
    {
        string Domain { get; }

        DateTimeOffset? Expires { get; }

        bool? HttpOnly { get; }

        double? MaxAge { get; }

        string Name { get; }

        string? Path { get; }

        SameSite SameSite { get; }

        bool? Secure { get; }

        string Value { get; }
    }
}
