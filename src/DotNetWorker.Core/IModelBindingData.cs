// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Core
{
    /// <summary>
    /// A representation of a Microsoft.Azure.WebJobs.ParameterBindingData
    /// </summary>
    public interface IModelBindingData
    {
        /// <summary>
        /// The version of the binding data content
        /// </summary>
        string Version { get; }

        /// <summary>
        /// The extension source of the binding data i.e CosmosDB, AzureStorageBlobs
        /// </summary>
        string Source { get; }

        /// <summary>
        /// The binding data content
        /// </summary>
        BinaryData Content { get; }

        /// <summary>
        /// The content type of the binding data content i.e. "application/json"
        /// </summary>
        string ContentType { get; }
    }
}
