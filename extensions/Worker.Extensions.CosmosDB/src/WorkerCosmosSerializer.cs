// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// Adapted from: https://github.com/Azure/azure-cosmos-dotnet-v3/blob/master/Microsoft.Azure.Cosmos/src/Serializer/CosmosJsonDotNetSerializer.cs

using System;
using System.IO;
using Azure.Core.Serialization;
using Microsoft.Azure.Cosmos;

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// This is a wrapper that allows us to use the worker options ObjectSerializer to create a CosmosSerializer.
    /// <summary>
    internal sealed class WorkerCosmosSerializer : CosmosSerializer
    {
        private readonly ObjectSerializer _serializer;

        /// <summary>
        /// Create a serializer that uses the Azure.Core ObjectSerializer
        /// </summary>
        public WorkerCosmosSerializer(ObjectSerializer? serializer)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        /// <summary>
        /// Convert a Stream to the passed in type.
        /// </summary>
        /// <typeparam name="T">The type of object that should be deserialized.</typeparam>
        /// <param name="stream">An open stream that is readable that contains JSON.</param>
        /// <returns>The object representing the deserialized stream.</returns>
        public override T FromStream<T>(Stream stream)
        {
            if (typeof(Stream).IsAssignableFrom(typeof(T)))
            {
                return (T)(object)stream;
            }

            using (stream)
            {
                return (T)_serializer.Deserialize(stream, typeof(T), default)!;
            }
        }

        /// <summary>
        /// Converts an object to an open readable stream.
        /// </summary>
        /// <typeparam name="T">The type of object being serialized.</typeparam>
        /// <param name="input">The object to be serialized.</param>
        /// <returns>An open readable stream containing the JSON of the serialized object.</returns>
        public override Stream ToStream<T>(T input)
        {
            MemoryStream streamPayload = new();
            _serializer.Serialize(streamPayload, input, typeof(T), default);
            streamPayload.Position = 0;
            return streamPayload;
        }
    }
}
