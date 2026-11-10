// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker
{
    internal static class ApplicationDirectory
    {
        public static string GetApplicationDirectory()
        {
            string? directory = Environment.GetEnvironmentVariable("FUNCTIONS_APPLICATION_DIRECTORY");
            return string.IsNullOrWhiteSpace(directory) ? AppContext.BaseDirectory : directory;
        }
    }
}
