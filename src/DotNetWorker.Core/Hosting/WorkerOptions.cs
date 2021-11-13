// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text.Json;
using Azure.Core.Serialization;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// An options class for configuring the worker.
    /// </summary>    
    public class WorkerOptions
    {
        /// <summary>
        /// The <see cref="ObjectSerializer"/> to use for all JSON serialization and deserialization. By default,
        /// this is a default <see cref="JsonObjectSerializer"/> with default <see cref="JsonSerializerOptions"/>.
        /// </summary>
        public ObjectSerializer? Serializer { get; set; }

        /// <summary>
        /// Gets the collection of input converters.
        /// </summary>
        public InputConverterCollection InputConverters { get; } = new InputConverterCollection();
    }
}
