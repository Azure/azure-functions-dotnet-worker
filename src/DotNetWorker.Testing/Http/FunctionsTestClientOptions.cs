// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Testing;

/// <summary>Configures a function-targeted built-in HTTP client.</summary>
public sealed class FunctionsHttpClientOptions
{
    /// <summary>Gets or sets the absolute HTTP or HTTPS base address.</summary>
    public Uri BaseAddress { get; set; } = new("http://localhost");

    internal void Validate() => ValidateBaseAddress(BaseAddress, nameof(BaseAddress));

    internal static void ValidateBaseAddress(Uri? address, string parameterName)
    {
        if (address is null
            || !address.IsAbsoluteUri
            || (address.Scheme != Uri.UriSchemeHttp && address.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("The base address must be an absolute HTTP or HTTPS URI.", parameterName);
        }
    }
}

/// <summary>Configures a companion-provided worker HTTP client.</summary>
public sealed class FunctionsTestClientOptions
{
    /// <summary>Gets or sets the absolute HTTP or HTTPS base address.</summary>
    public Uri BaseAddress { get; set; } = new("http://localhost");

    /// <summary>Gets or sets whether redirects are followed.</summary>
    public bool AllowAutoRedirect { get; set; } = true;

    /// <summary>Gets or sets whether cookies are handled.</summary>
    public bool HandleCookies { get; set; } = true;

    internal void Validate() => FunctionsHttpClientOptions.ValidateBaseAddress(BaseAddress, nameof(BaseAddress));
}
