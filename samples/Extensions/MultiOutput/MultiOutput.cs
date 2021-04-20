// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace SampleApp
{
    /// <summary>
    /// This class specifies output bindings in the properties of <see cref="MyOutputType"/>.
    /// <see cref="MyOutputType"/> defines a Queue output binding, and an Http Response property.
    /// By default, a property of type <see cref="HttpResponseData"/> in the return type of the function
    /// is treated as an Http output binding. This property can be used to provide a response to the Http trigger.
    /// </summary>
    //<docsnippet_multiple_outputs>
    public static class MultiOutput
    {
        [Function("MultiOutput")]
        public static MyOutputType Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req,
            FunctionContext context)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.WriteString("Success!");

            string myQueueOutput = "some output";

            return new MyOutputType()
            {
                Name = myQueueOutput,
                HttpResponse = response
            };
        }
    }

    public class MyOutputType
    {
        [QueueOutput("myQueue")]
        public string Name { get; set; }

        public HttpResponseData HttpResponse { get; set; }
    }
    //</docsnippet_multiple_outputs>
}
