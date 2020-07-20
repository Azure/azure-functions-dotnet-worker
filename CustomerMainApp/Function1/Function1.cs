
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System.Text.Json;
using FunctionsDotNetWorker;

namespace CustomerMainApp
{
    public static class Function1
    {
        
        [FunctionName("Function1")]
        public static Book Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req, string myBlob)
        {
            return (Book)JsonSerializer.Deserialize(myBlob, typeof(Book)); ;
        }

        public class Book
        {
            public string name { get; set; }
            public string id { get; set; }
        }

    }
}
