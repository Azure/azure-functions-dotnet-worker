// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker
{
    [AttributeUsage(AttributeTargets.Method)]
    public abstract class RetryAttribute : Attribute
    {
        public RetryAttribute()
        {
        }

        /// <summary>
        /// The maximum number of retries allowed per function execution
        /// </summary>
        public int MaxRetryCount { get; set; }
    }
}
