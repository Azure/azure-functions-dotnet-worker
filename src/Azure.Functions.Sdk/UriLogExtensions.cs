// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Azure.Functions.Sdk;

/// <summary>
/// Extensions for producing log-safe representations of URLs.
/// </summary>
/// <remarks>
/// Deployment/publish URLs can carry secrets - credentials embedded as user info
/// (<c>https://user:password@host</c>) or tokens in the query string (for example a
/// SAS <c>?sig=...</c>). Build logs are frequently captured by CI systems and shared,
/// so those components are stripped before a URL is written to the log.
/// </remarks>
internal static class UriLogExtensions
{
    extension(Uri? uri)
    {
        /// <summary>
        /// Gets a representation of the URI that is safe to write to build logs. The user
        /// info (credentials), query string, and fragment are removed because they may
        /// contain secrets; only the scheme, host, optional port, and path are retained.
        /// </summary>
        /// <returns>A redacted URI string, or an empty string when <paramref name="uri"/> is <c>null</c>.</returns>
        public string ToLogSafeString()
        {
            if (uri is null)
            {
                return string.Empty;
            }

            if (!uri.IsAbsoluteUri)
            {
                // Relative URIs have no authority, but may still carry a query or fragment;
                // redact them best-effort so nothing after the path is logged.
                return RedactRawUrl(uri.ToString());
            }

            UriBuilder builder = new()
            {
                Scheme = uri.Scheme,
                Host = uri.Host,
                Port = uri.IsDefaultPort ? -1 : uri.Port,
                Path = uri.AbsolutePath,
            };

            return builder.Uri.ToString();
        }
    }

    extension(string? url)
    {
        /// <summary>
        /// Gets a representation of the URL string that is safe to write to build logs.
        /// When the value is an absolute URI the user info, query string, and fragment are
        /// removed; otherwise a best-effort redaction strips any user info and query/fragment.
        /// </summary>
        /// <returns>A redacted URL string.</returns>
        public string ToLogSafeString()
        {
            if (string.IsNullOrEmpty(url))
            {
                return url ?? string.Empty;
            }

            return Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)
                ? uri.ToLogSafeString()
                : RedactRawUrl(url!);
        }
    }

    /// <summary>
    /// Best-effort redaction for values that do not parse as an absolute URI. Removes any
    /// query string or fragment (which may contain secrets) and any user info component.
    /// </summary>
    private static string RedactRawUrl(string url)
    {
        // Drop the query and/or fragment, which are the most likely places for secrets.
        int queryOrFragment = url.IndexOfAny(['?', '#']);
        string result = queryOrFragment >= 0 ? url[..queryOrFragment] : url;

        // Strip user info (anything between the "//" authority marker and the "@").
        int authorityStart = result.IndexOf("//", StringComparison.Ordinal);
        int start = authorityStart >= 0 ? authorityStart + 2 : 0;
        int at = result.IndexOf('@', start);
        if (at >= 0)
        {
            result = result[..start] + result[(at + 1)..];
        }

        return result;
    }
}
