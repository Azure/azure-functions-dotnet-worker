// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    public static class QueueFunction
    {
        [Function("QueueFunction")]
        [QueueOutput("functionstesting2")]
        public static string Run([QueueTrigger("functionstesting2")] Book myQueueItem,
            FunctionContext context)
        {
            var logger = context.GetLogger("QueueFunction");
            logger.LogInformation($"Book name = {myQueueItem.Name}");

            // Queue Output
            return "queue message";
        }
    }

    public class Book
    {
        public string Name { get; set; }

        public string Id { get; set; }
    }
}
