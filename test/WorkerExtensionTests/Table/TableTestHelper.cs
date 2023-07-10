// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Google.Protobuf;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;

namespace Microsoft.Azure.Functions.WorkerExtension.Tests
{
    internal static class TableTestHelper
    {
        public static BinaryData GetWrongBinaryData()
        {
            return new BinaryData("{" + "\"Connection\" : \"Connection\"" + "}");
        }

        public static BinaryData GetTableClientBinaryData()
        {
            return new BinaryData("{" +
                "\"TableName\" : \"TableName\"" +
                "}");
        }

        public static BinaryData GetTableEntityBinaryData()
        {
            return new BinaryData("{" +
                "\"Connection\" : \"Connection\"," +
                "\"TableName\" : \"TableName\"," +
                "\"PartitionKey\" : \"PartitionKey\"," +
                "\"RowKey\" : \"RowKey\"" +
                "}");
        }

        public static BinaryData GetBadEntityBinaryData()
        {
            return new BinaryData("{" +
                "\"Connection\" : \"Connection\"," +
                "\"TableName\" : \"TableName\"," +
                "\"PartitionKey\" : \"PartitionKey\"" +
                "}");
        }


        public static GrpcModelBindingData GetTestGrpcModelBindingData(BinaryData binaryData, string source = "AzureStorageTables", string contentType = "application/json")
        {
            return new GrpcModelBindingData(new ModelBindingData()
            {
                Version = "1.0",
                Source = source,
                Content = ByteString.CopyFrom(binaryData),
                ContentType = contentType
            });
        }
    }
}