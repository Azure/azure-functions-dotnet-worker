// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore
{
    internal static class HttpUriProvider
    {
        private static Lazy<Uri> HttpUri = new Lazy<Uri>(() => new Uri("http://localhost:" + Utilities.GetUnusedTcpPort().ToString()));

        public static string HttpUriString { get; } = HttpUri.Value.ToString();
    }
}
