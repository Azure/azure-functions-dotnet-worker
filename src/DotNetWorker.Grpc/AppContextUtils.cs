// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Grpc
{
    internal class AppContextUtils
    {
        /// <summary>
        /// Returns a boolean value indicating whether the worker is running with native host(placeholder support).
        /// </summary>
        internal static bool IsRunningWithNativeHost() => AppContext.GetData("AZURE_FUNCTIONS_NATIVE_HOST") is not null;
    }
}
