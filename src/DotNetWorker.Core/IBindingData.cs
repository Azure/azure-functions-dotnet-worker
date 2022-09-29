// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker.Core
{
    /// <summary>
    /// A representation of a Binding Data
    /// </summary>
    public interface IBindingData
    {
        /// <summary>
        /// Version of ParameterBindingData schema
        /// </summary>
        string Version { get; }

        /// <summary>
        /// Indicates the original media type of the resource i.e. text/plain or application/json
        /// </summary>
        string ContentType { get; }

        /// <summary>
        /// Extension source i.e CosmosDB, BlobStorage
        /// </summary>
        string Source { get; }

        /// <summary>
        /// A string containing any required information to hydrate
        /// an SDK-type object in the out-of-process worker
        /// </summary>
        string Content { get; }
    }
}
