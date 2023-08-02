// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text.Json;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker.Storage.Queues;
using Xunit;

namespace Microsoft.Azure.Functions.Worker.Extensions.Tests.Queue
{
    public class QueueMessageJsonConverterTests
    {
        [Fact]
        public void QueueMessageJsonConverter_ValidJson_ReturnsQueueMessage()
        {
            var data = GetTestBinaryData();

            JsonSerializerOptions options = new() { Converters = { new QueueMessageJsonConverter() } };
            var result = data.ToObjectFromJson<QueueMessage>(options);

            Assert.Equal(typeof(QueueMessage), result.GetType());
        }

        private BinaryData GetTestBinaryData(string messageId = "fbb84c41-9f1f-4c75-950c-72d0541fb8ae", string message = "hello world")
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
    }
}
