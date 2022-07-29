// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.Logging
{
    /// <summary>
    /// An abstraction for writing user metrics.
    /// </summary>
    internal interface IUserMetricWriter
    {
        /// <summary>
        /// Writes user metrics.
        /// </summary>
        /// <param name="scopeProvider">The provider of scope data.</param>
        /// <param name="state">Additional properties.</param>        
        void WriteUserMetric(IExternalScopeProvider scopeProvider, IDictionary<string, object> state);
    }
}
