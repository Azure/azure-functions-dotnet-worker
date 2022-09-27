// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    public static class BlobStreamFunction
    {
        [Function(nameof(BlobStreamFunction))]
        public static async Task Run(
            [BlobTrigger("blobstream-trigger/{name}", Connection = "AzureWebJobsStorage")] Stream stream,
            FunctionContext context)
        {
            var logger = context.GetLogger(nameof(BlobStreamFunction));
            using var blobStreamReader = new StreamReader(stream);
            logger.LogInformation("Blob content: {stream}", blobStreamReader.ReadToEnd());
        }
    }
}
