// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    public static class QueueFunction
    {
        //<docsnippet_queue_output_binding>
        //<docsnippet_queue_trigger>
        [Function("QueueFunction")]
        [QueueOutput("myqueue-output")]
        public static string Run([QueueTrigger("myqueue-items")] Book myQueueItem,
            FunctionContext context)
        //</docsnippet_queue_trigger>
        {
            var logger = context.GetLogger("QueueFunction");
            logger.LogInformation($"Book name = {myQueueItem.Name}");

            // Queue Output
            return "queue message";
        }
        //</docsnippet_queue_output_binding>
    }

    public class Book
    {
        public string Name { get; set; }

        public string Id { get; set; }
    }
}
