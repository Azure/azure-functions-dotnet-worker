using System;
using Microsoft.Azure.WebJobs;

namespace CustomerMainApp
{
    public static class Function2
    {
        [FunctionName("Function2")]
        public static Book Run([QueueTrigger("functionstesting2", Connection = "AzureWebJobsStorage")] Book myQueueItem,
            string myBlob)
        {
            Console.WriteLine(myBlob);
            return myQueueItem;
        }
    }

    public class Book
    {
        public string name { get; set; }
        public string id { get; set; }
    }

}
