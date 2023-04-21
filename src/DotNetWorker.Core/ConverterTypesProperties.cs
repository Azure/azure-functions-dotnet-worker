// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Core
{
    /// <summary>
    /// A representation of a Microsoft.Azure.WebJobs.ParameterBindingData
    /// </summary>
    public class ConverterTypeProperties
    {
        /// <summary>
        /// Gets the version of the binding data content
        /// </summary>
        public Type SupportedType { get; set; }

        /// <summary>
        /// Gets the extension source of the binding data i.e CosmosDB, AzureStorageBlobs
        /// </summary>
        public bool SupportsCollection { get; set; }
    }
}
