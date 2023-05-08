// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker.Extensions.Abstractions
{
    /// <summary>
    /// Class containing constant values representing known binding capabilities.
    /// </summary>
    public static class KnownBindingCapabilities
    {
        /// <summary>
        /// Signals that a binding has function level retry support.
        /// </summary>
        public const string FunctionLevelRetry = "FunctionLevelRetry";
    }
}
