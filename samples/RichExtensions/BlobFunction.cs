// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace SampleApp
{
    public static class BlobFunction
    {
        [Function("BlobFunction")]
        public static void Run(
            [BlobTrigger("test-trigger/{name}")] BlobClient client,
            FunctionContext context)
        {
            var logger = context.GetLogger("BlobFunction");
        }
    }
}
