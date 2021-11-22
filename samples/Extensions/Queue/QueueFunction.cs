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
        [QueueOutput("output-queue")]
        public static string[] Run([QueueTrigger("input-queue")] Book myQueueItem,

            FunctionContext context)
        //</docsnippet_queue_trigger>
        {
            // Use a string array to return more than one message.
            string[] messages = {
                $"Book name = {myQueueItem.Name}",
                $"Book ID = {myQueueItem.Id}"};
            var logger = context.GetLogger("QueueFunction");
            logger.LogInformation($"{messages[0]},{messages[1]}");

            // Queue Output messages
            return messages;
        }
        //</docsnippet_queue_output_binding>
    }

    public class Book
    {
        public string Name { get; set; }

        public string Id { get; set; }
    }
}
