// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Net;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.Functions.Worker.Extensions.Http;
using Microsoft.Azure.Functions.Worker.Extensions.Storage;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.E2EApp.Queue
{
    public class QueueTestFunctions
    {
        [FunctionName("QueueTriggerAndOutput")]
        [QueueOutput("output", "test-output-dotnet-isolated")]
        public static void QueueTriggerAndOutput([QueueTrigger("test-input-dotnet-isolated")] string message,
            FunctionContext context)
        {
            var logger = context.GetLogger<QueueTestFunctions>();
            logger.LogInformation($"Message: {message}");

            context.OutputBindings["output"] = message;
        }

        [FunctionName("QueueOutputPocoList")]
        [QueueOutput("output", "test-output-dotnet-isolated-poco")]
        public HttpResponseData QueueOutputPocoList(
            [HttpTrigger()] HttpRequestData request,
            FunctionContext context)
        {
            context.GetLogger<QueueTestFunctions>().LogInformation(".NET HTTP trigger processed a request.");

            var query = QueryHelpers.ParseQuery(request.Url.Query);
            string queueMessageId = query["queueMessageId"];
            var outputItems = new List<TestData>();

            if (queueMessageId != null)
            {
                TestData testData1 = new TestData
                {
                    Id = "msg1" + queueMessageId
                };

                TestData testData2 = new TestData
                {
                    Id = "msg2" + queueMessageId
                };

                outputItems.Add(testData1);
                outputItems.Add(testData2);

                context.OutputBindings["output"] = outputItems;

                return new HttpResponseData(HttpStatusCode.OK, queueMessageId);
            }
            else
            {
                return new HttpResponseData(HttpStatusCode.BadRequest);
            }
        }

        [FunctionName("QueueTriggerAndOutputPoco")]
        [QueueOutput("output", "test-output-dotnet-isolated-poco")]
        public void QueueTriggerAndOutputPoco(
            [QueueTrigger("test-input-dotnet-isolated-poco")] TestData message,
            FunctionContext context)
        {
            context.GetLogger<QueueTestFunctions>().LogInformation(".NET Queue trigger POCO function processed a message: " + message.Id);
            context.OutputBindings["output"] = message;
        }

        [FunctionName("QueueTriggerMetadata")]
        [QueueOutput("output", "test-output-dotnet-isolated-metadata")]
        public void QueueTriggerMetadata(
            [QueueTrigger("test-input-dotnet-isolated-metadata")] string message, string id,
            FunctionContext context)
        {
            context.GetLogger<QueueTestFunctions>().LogInformation(".NET Queue trigger function processed a message: " + message + " whith metadaId:" + id);

            TestData testData = new TestData
            {
                Id = id
            };
            context.OutputBindings["output"] = testData;
        }

        public class TestData
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }
        }
    }
}
