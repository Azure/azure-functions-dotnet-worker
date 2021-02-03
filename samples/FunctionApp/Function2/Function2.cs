// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.WebJobs;

namespace FunctionApp
{
    public static class Function2
    {
        [FunctionName("Function2")]
        public static Book Run([QueueTrigger("functionstesting2", Connection = "AzureWebJobsStorage")] Book myQueueItem,
            [Blob("test-samples/sample1.txt", Connection = "AzureWebJobsStorage")] string myBlob)
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
