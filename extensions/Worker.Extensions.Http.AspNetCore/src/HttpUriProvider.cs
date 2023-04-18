// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore
{
    internal static class HttpUriProvider
    {
        public static Lazy<Uri> HttpUri = new Lazy<Uri>(() => GetHttpUri());

        public static Lazy<int> HttpPort = new Lazy<int>(() => Utilities.GetUnusedTcpPort());

        public static Lazy<string> HttpUriString = new Lazy<string>(() => HttpUri.Value.ToString());

        public static Uri GetHttpUri()
        {
            // TODO: replace local host string
            var uriString = "http://localhost:" + HttpPort.Value.ToString();

            return new Uri(uriString);
        }
    }
}
