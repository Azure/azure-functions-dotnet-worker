// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Google.Protobuf;
using Microsoft.Azure.Functions.Worker.Grpc.Messages;
using Xunit;

namespace Microsoft.Azure.Functions.WorkerExtension.Tests
{
    internal static class QueuesTestHelper
    {
        public static BinaryData GetTestBinaryData(string messageId = "fbb84c41-9f1f-4c75-950c-72d0541fb8ae", string message = "hello world")
        {
            string jsonData = $@"{{
                                ""MessageId"" : ""{messageId}"",
                                ""PopReceipt"" : ""AgAAAAMAAAAAAAAASm\u002B7xBZv2QE="",
                                ""MessageText"" : ""{message}"",
                                ""Body"" : {{}},
                                ""NextVisibleOn"" : ""2023-04-14T21:19:16+00:00"",
                                ""InsertedOn"" : ""2023-04-14T21:09:14+00:00"",
                                ""ExpiresOn"" : ""2023-04-21T21:09:14+00:00"",
                                ""DequeueCount"" : 1
                            }}";

            return new BinaryData(jsonData);
        }

        public static GrpcModelBindingData GetTestGrpcModelBindingData(BinaryData content = null, string source = "AzureStorageQueues", string contentType = "application/json")
        {
            if (content == null)
            {
                content = GetTestBinaryData();
            }

            var data = new ModelBindingData()
            {
                Version = "1.0",
                Source = source,
                Content = ByteString.CopyFrom(content),
                ContentType = contentType
            };

            return new GrpcModelBindingData(data);
        }
    }
}
