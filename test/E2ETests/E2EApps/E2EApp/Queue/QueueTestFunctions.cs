// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Net;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.E2EApp.Queue
{
    public class QueueTestFunctions
    {
        [Function("QueueTriggerAndOutput")]
        [QueueOutput("test-output-dotnet-isolated")]
        public string QueueTriggerAndOutput([QueueTrigger("test-input-dotnet-isolated")] string message,
            FunctionContext context)
        {
            var logger = context.GetLogger<QueueTestFunctions>();
            logger.LogInformation($"Message: {message}");

            return message;
        }

        [Function("QueueTriggerAndArrayOutput")]
        [QueueOutput("test-output-array-dotnet-isolated")]
        public string[] QueueTriggerAndArrayOutput([QueueTrigger("test-input-array-dotnet-isolated")] string message,
            FunctionContext context)
        {
            var logger = context.GetLogger<QueueTestFunctions>();
            logger.LogInformation($"Message: {message}");

            return new string[] {
                message + "-1",
                message + "-2"
            };
        }

        [Function("QueueTriggerAndListOutput")]
        [QueueOutput("test-output-list-dotnet-isolated")]
        public List<string> QueueTriggerAndListOutput([QueueTrigger("test-input-list-dotnet-isolated")] string message,
            FunctionContext context)
        {
            var logger = context.GetLogger<QueueTestFunctions>();
            logger.LogInformation($"Message: {message}");

            return new List<string>() {
                message + "-1",
                message + "-2"
            };
        }

        [Function("QueueOutputPocoList")]
        public HttpAndQueue QueueOutputPocoList(
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

                HttpResponseData response = request.CreateResponse(HttpStatusCode.OK);
                response.WriteString(queueMessageId);

                return new HttpAndQueue()
                {
                    MyHttpData = response,
                    MyQueueOutput = outputItems
                };
            }
            else
            {
                return new HttpAndQueue()
                {
                    MyHttpData = request.CreateResponse(HttpStatusCode.BadRequest)
                };
            }
        }

        [Function("QueueTriggerAndOutputPoco")]
        [QueueOutput("test-output-dotnet-isolated-poco")]
        public TestData QueueTriggerAndOutputPoco(
            [QueueTrigger("test-input-dotnet-isolated-poco")] TestData message,
            FunctionContext context)
        {
            context.GetLogger<QueueTestFunctions>().LogInformation(".NET Queue trigger POCO function processed a message: " + message.Id);
            return message;
        }

        [Function("QueueTriggerMetadata")]
        [QueueOutput("test-output-dotnet-isolated-metadata")]
        public TestData QueueTriggerMetadata(
            [QueueTrigger("test-input-dotnet-isolated-metadata")] string message, string id,
            FunctionContext context)
        {
            context.GetLogger<QueueTestFunctions>().LogInformation(".NET Queue trigger function processed a message: " + message + " whith metadaId:" + id);

            TestData testData = new TestData
            {
                Id = id
            };
            return testData;
        }

        public class TestData
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }
        }

        public class HttpAndQueue
        {
            public HttpResponseData MyHttpData { get; set; }

            [QueueOutput("test-output-dotnet-isolated-poco")]
            public List<TestData> MyQueueOutput { get; set; }
        }
    }
}
