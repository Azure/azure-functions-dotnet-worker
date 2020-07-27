using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using FunctionsDotNetWorker;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CustomerMainApp
{
    public static class Function3
    {

        [FunctionName("Function3")]
        public static HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req, 
            OutputBinding<string> name )
        {
            var response = new HttpResponseData();
            response.StatusCode = "200";
            response.Body = "Success!!";

            name.SetValue("some name");

            return response;
        }
    }

}
