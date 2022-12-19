// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker.Core
{
    /// <summary>
    /// A representation of Collection of Microsoft.Azure.WebJobs.ParameterBindingData
    /// </summary>
    public abstract class CollectionModelBindingData
    {
        /// <summary>
        /// Gets the array of ModelBindingData
        /// </summary>
        public abstract ModelBindingData[] ModelBindingDataArray { get; }
    }
}
