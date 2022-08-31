// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Runtime;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Azure.Functions.Worker.Converters;
using System.Net.Mime;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    /// <summary>
    /// Converter to bind Blob Storage type parameters.
    /// </summary>
    internal class BlobStorageConverter : IInputConverter
    {
        public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            if (context.TargetType != typeof(BlobClient))
            {
                return new ValueTask<ConversionResult>(ConversionResult.Unhandled());
            }

            object? client = null;

            var referenceData = JObject.Parse(context?.Source?.ToString());
            var blob = referenceData?["Properties"]["blob_name"].ToString();
            var container = referenceData?["Properties"]["blob_container"].ToString();
            var connection = referenceData?["Properties"]["connection_string"].ToString();

            if(string.IsNullOrEmpty(connection))
            {
                connection = "AzureWebJobsStorage";
            }

            var connectionString = Environment.GetEnvironmentVariable(connection);

            switch (context.TargetType)
            {
                // TODO: simplify creation using generics i.e.
                // await GetBlobAsync(blobAttribute, cancellationToken, typeof(T)).ConfigureAwait(false);
                case Type _ when context.TargetType == typeof(BlobClient):
                    client = new BlobClient(connectionString, container, blob);
                    break;

                case Type _ when context.TargetType == typeof(AppendBlobClient):
                    client = new AppendBlobClient(connectionString, container, blob);
                    break;

                case Type _ when context.TargetType == typeof(BlockBlobClient):
                    client = new BlockBlobClient(connectionString, container, blob);
                    break;

                case Type _ when context.TargetType == typeof(PageBlobClient):
                    client = new PageBlobClient(connectionString, container, blob);
                    break;

                case Type _ when context.TargetType == typeof(System.IO.Stream):
                    BlobClient blobClient = new BlobClient(connectionString, container, blob);
                    var result = blobClient.DownloadStreaming();
                    var content = result.Value.Content;
                    client = content;
                    break;
            }

            if (client is not null)
            {
                return new ValueTask<ConversionResult>(ConversionResult.Success(client));
            }

            return new ValueTask<ConversionResult>(ConversionResult.Unhandled());
        }
    }
}
