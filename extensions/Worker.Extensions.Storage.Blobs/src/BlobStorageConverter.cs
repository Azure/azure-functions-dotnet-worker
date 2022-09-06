// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
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
using Microsoft.Azure.Functions.Worker.Core;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    /// <summary>
    /// Converter to bind Blob Storage type parameters.
    /// </summary>
    internal class BlobStorageConverter : IInputConverter
    {
        public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            object? result = null;

            if (context.Source is not IBindingData bindingData)
            {
                return new ValueTask<ConversionResult>(ConversionResult.Unhandled());
            }

            var blob = bindingData.Properties["blob_name"];
            var container = bindingData.Properties["blob_container"];
            var connection = bindingData.Properties["connection_name"];

            var connectionString = Environment.GetEnvironmentVariable(connection);

            switch (context.TargetType)
            {
                case Type _ when context.TargetType == typeof(Stream):
                    BlobClient blobClient = new BlobClient(connectionString, container, blob);
                    var downloadResult = blobClient.DownloadStreaming();
                    result = downloadResult.Value.Content;
                    break;
                
                case Type _ when context.TargetType == typeof(BinaryData):
                    blobClient = new BlobClient(connectionString, container, blob);
                    var stream = new MemoryStream();
                    blobClient.DownloadTo(stream);
                    result = stream.ToArray();
                    break;

                case Type _ when context.TargetType == typeof(String):
                    blobClient = new BlobClient(connectionString, container, blob);
                    var downloadResultInString = blobClient.DownloadContent();
                    result = downloadResultInString.Value.Content.ToString();
                    break;

                // TODO: simplify creation using generics i.e.
                // await GetBlobAsync(blobAttribute, cancellationToken, typeof(T)).ConfigureAwait(false);
                case Type _ when context.TargetType == typeof(BlobClient):
                    result = new BlobClient(connectionString, container, blob);
                    break;

                case Type _ when context.TargetType == typeof(AppendBlobClient):
                    result = new AppendBlobClient(connectionString, container, blob);
                    break;

                case Type _ when context.TargetType == typeof(BlockBlobClient):
                    result = new BlockBlobClient(connectionString, container, blob);
                    break;

                case Type _ when context.TargetType == typeof(PageBlobClient):
                    result = new PageBlobClient(connectionString, container, blob);
                    break;
            }

            if (result is not null)
            {
                return new ValueTask<ConversionResult>(ConversionResult.Success(result));
            }

            return new ValueTask<ConversionResult>(ConversionResult.Unhandled());
        }
    }
}
