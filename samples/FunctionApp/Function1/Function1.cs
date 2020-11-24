
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace FunctionApp
{
    public static class Function1
    {

        [FunctionName("Function1")]
        public static HttpResponseData Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req, [Blob("test-samples/sample1.txt", Connection = "AzureWebJobsStorage")] string myBlob,
              [Queue("functionstesting2", Connection = "AzureWebJobsStorage")] OutputBinding<Book> book)
        {
            var bookVal = (Book)JsonSerializer.Deserialize(myBlob, typeof(Book));
            book.SetValue(bookVal);
            var response = new HttpResponseData();
            var headers = new Dictionary<string, string>();
            headers.Add("Date", "Mon, 18 Jul 2016 16:06:00 GMT");
            headers.Add("Content", "Content - Type: text / html; charset = utf - 8");

            response.Headers = headers;
            response.StatusCode = "200";
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
