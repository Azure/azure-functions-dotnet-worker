// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.


using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.Functions.Worker.Extensions.Http;
using Microsoft.Azure.Functions.Worker.Extensions.Storage;
using Microsoft.Azure.Functions.Worker.Pipeline;

namespace FunctionApp
{
    public static class Function1
    {

        [FunctionName("Function1")]
        [QueueOutput("book", "functionstesting2", Connection = "AzureWebJobsStorage")]
        public static HttpResponseData Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req,
            [BlobInput("test-samples/sample1.txt", Connection = "AzureWebJobsStorage")] string myBlob, FunctionExecutionContext context)
        {
            var bookVal = (Book)JsonSerializer.Deserialize(myBlob, typeof(Book));
            context.OutputBindings["book"] = bookVal;

            var response = new HttpResponseData(HttpStatusCode.OK);
            var headers = new Dictionary<string, string>();
            headers.Add("Date", "Mon, 18 Jul 2016 16:06:00 GMT");
            headers.Add("Content", "Content - Type: text / html; charset = utf - 8");

            response.Headers = headers;
            response.Body = "Book Sent to Queue!";

            return response;
        }

        public class Book
        {
            public string name { get; set; }
            public string id { get; set; }
        }

    }
}
