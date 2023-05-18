// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Core.FunctionMetadata
{
    /// <summary>
    /// 
    /// </summary>
    public class DefaultRetryOptions : IRetryOptions
    {
        /// <inheritdoc/>
        public int? MaxRetryCount { get; set; }

        /// <inheritdoc/>
        public string? DelayInterval { get; set; }

        /// <inheritdoc/>
        public string? MinimumInterval { get; set; }

        /// <inheritdoc/>
        public string? MaximumInterval { get; set; }

        /// <inheritdoc/>
        public string? Strategy { get; set; }
    }
}
