﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore
{
    internal static class HttpUriProvider
    {
        private static Uri? _httpUri;

        public static Lazy<int> HttpPort = new Lazy<int>(() => Utilities.GetUnusedTcpPort());

        public static Uri GetHttpUri()
        {
            if (_httpUri is not null)
            {
                return _httpUri;
            }

            // TODO: replace local host string
            var uriString = "http://localhost:" + HttpPort.Value.ToString();

            _httpUri = new Uri(uriString);

            return _httpUri;
        }

        public static string GetHttpUriAsString()
        {
            return GetHttpUri().ToString();
        }
    }
}
