// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Text.Json;
using Azure.Core.Serialization;

namespace Microsoft.Azure.Functions.Worker
{
    public class WorkerOptions
    {
        private ObjectSerializer? _serializer;

        /// <summary>
        /// The <see cref="ObjectSerializer"/> to use for all JSON serialization and deserialization. By default,
        /// this is a default <see cref="JsonObjectSerializer"/> with default <see cref="JsonSerializerOptions"/>.
        /// </summary>
        public ObjectSerializer Serializer
        {
            get => _serializer ??= new JsonObjectSerializer();
            set => _serializer = value;
        }
    }
}
