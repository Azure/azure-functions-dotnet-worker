// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker.Logging
{
    /// <summary>
    /// Constant values for log categories.
    /// </summary>
    internal static class LogCategories
    {
        /// <summary>
        /// The category for logs written for a specific function invocation.
        /// </summary>
        internal static string CreateFunctionCategory(string functionName) => $"Function.{functionName}";

        /// <summary>
        /// The category for logs written from within user functions.
        /// </summary>
        internal static string CreateFunctionUserCategory(string functionName) => $"{CreateFunctionCategory(functionName)}.User";
    }
}
