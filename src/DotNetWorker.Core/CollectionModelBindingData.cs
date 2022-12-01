// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker.Core
{
    /// <summary>
    /// A representation of a Microsoft.Azure.WebJobs.ParameterBindingData
    /// </summary>
    public abstract class CollectionModelBindingData
    {
        /// <summary>
        /// Gets the version of the binding data content
        /// </summary>
        public abstract ModelBindingData[] modelBindingDataArray { get; }
    }
}