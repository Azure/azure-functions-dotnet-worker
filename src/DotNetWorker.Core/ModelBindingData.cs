// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Core
{
    /// <summary>
    /// A representation of a Microsoft.Azure.WebJobs.ParameterBindingData
    /// </summary>
    public abstract class ModelBindingData
    {
        /// <summary>
        /// Gets the version of the binding data content
        /// </summary>
        public abstract string Version { get; }

        /// <summary>
        /// Gets the extension source of the binding data i.e CosmosDB, AzureStorageBlobs
        /// </summary>
        public abstract string Source { get; }

        /// <summary>
        /// Gets the binding data content
        /// </summary>
        public abstract BinaryData Content { get; }

        /// <summary>
        /// Gets the content type of the binding data content i.e. "application/json"
        /// </summary>
        public abstract string ContentType { get; }
    }
}
