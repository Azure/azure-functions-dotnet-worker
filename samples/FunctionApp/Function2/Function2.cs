// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using Microsoft.Azure.Functions.Worker.Extensions.Storage;

namespace FunctionApp
{
    public static class Function2
    {
        [Function("Function2")]
        public static Book Run([QueueTrigger("functionstesting2", Connection = "AzureWebJobsStorage")] Book myQueueItem,
            [BlobInput("test-samples/sample1.txt", Connection = "AzureWebJobsStorage")] string myBlob)
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
