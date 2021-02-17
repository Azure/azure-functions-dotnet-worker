// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text.Json;
using Azure.Core.Serialization;

namespace Microsoft.Azure.Functions.Worker
{
    public class WorkerOptions
    {
        public WorkerOptions()
        {
            var serializerOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            Serializer = new JsonObjectSerializer(serializerOptions);
        }

        /// <summary>
        /// The <see cref="ObjectSerializer"/> to use for all JSON serialization and deserialization. By default,
        /// this is a <see cref="JsonObjectSerializer"/> with <see cref="JsonSerializerOptions.PropertyNameCaseInsensitive"/> set to true.        
        /// </summary>
        public ObjectSerializer Serializer { get; set; }
    }
}
