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

        /// <summary>
        /// Gets and sets the flag for opting in to unwrapping user-code-thrown
        /// exceptions when they are surfaced to the Host. 
        /// </summary>
        public bool EnableUserCodeException { get; set; } = false;
        
        /// <summary>
        /// Gets or sets a value that determines if empty entries should be included in the function trigger message payload.
        /// For example, if a set of entries were sent to a messaging service such as Service Bus or Event Hub and your function
        /// app has a Service bus trigger or Event hub trigger, only the non-empty entries from the payload will be sent to the
        /// function code as trigger data when this setting value is <see langword="false"/>. When it is <see langword="true"/>,
        /// All entries will be sent to the function code as it is. Default value for this setting is <see langword="false"/>.
        /// </summary>
        public bool IncludeEmptyEntriesInMessagePayload { get; set; }
    }
}
