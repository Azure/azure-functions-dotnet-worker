// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    public static class BlobTriggerWithBytesInput
    {
        [Function(nameof(BlobTriggerWithBytesInput))]
        public static async Task Run(
            [BlobTrigger("blobbinary-trigger/{name}", Connection = "AzureWebJobsStorage")] BinaryData data,
            FunctionContext context)
        {
            var logger = context.GetLogger(nameof(BlobTriggerWithBytesInput));
            logger.LogInformation($"Blob content: {Encoding.Default.GetString(data)}");
        }
    }
}
