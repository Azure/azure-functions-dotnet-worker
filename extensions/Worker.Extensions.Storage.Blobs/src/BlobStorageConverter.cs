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
            if (context.Source is not IBindingData bindingData)
            {
                return new ValueTask<ConversionResult>(ConversionResult.Unhandled());
            }

            var blobName = bindingData.Properties["blob_name"];
            var containerName = bindingData.Properties["blob_container"];
            var connectionName = bindingData.Properties["connection_name"];
            var connectionString = Environment.GetEnvironmentVariable(connectionName);

            object result = ToTargetType(context.TargetType, connectionString, connectionName, blobName);

            if (result is not null)
            {
                return new ValueTask<ConversionResult>(ConversionResult.Success(result));
            }

            return new ValueTask<ConversionResult>(ConversionResult.Unhandled());
        }

        private object? ToTargetType(Type targetType, string connectionString, string containerName, string blobName) => targetType switch
        {
            Type _ when targetType == typeof(String)            => GetBlobString(connectionString, containerName, blobName),
            Type _ when targetType == typeof(Stream)            => GetBlobStream(connectionString, containerName, blobName),
            Type _ when targetType == typeof(BinaryData)        => GetBlobBinaryData(connectionString, containerName, blobName),
            Type _ when targetType == typeof(BlobClient)        => CreateBlobReference<BlobClient>(connectionString, containerName, blobName),
            Type _ when targetType == typeof(BlockBlobClient)   => CreateBlobReference<BlockBlobClient>(connectionString, containerName, blobName),
            Type _ when targetType == typeof(PageBlobClient)    => CreateBlobReference<PageBlobClient>(connectionString, containerName, blobName),
            Type _ when targetType == typeof(AppendBlobClient)  => CreateBlobReference<AppendBlobClient>(connectionString, containerName, blobName),
            _ => null
        };

        private string GetBlobString(string connectionString, string containerName, string blobName)
        {
            var client = CreateBlobReference<BlobClient>(connectionString, containerName, blobName);
            var download = client.DownloadContent();
            return download.Value.Content.ToString();
        }

        private byte[] GetBlobBinaryData(string connectionString, string containerName, string blobName)
        {
            var client = CreateBlobReference<BlobClient>(connectionString, containerName, blobName);
            var stream = new MemoryStream();
            client.DownloadTo(stream);
            return stream.ToArray();
        }

        private Stream GetBlobStream(string connectionString, string containerName, string blobName)
        {
            var client = CreateBlobReference<BlobClient>(connectionString, containerName, blobName);
            var download = client.DownloadStreaming();
            return download.Value.Content;
        }

        private T CreateBlobReference<T>(string connectionString, string containerName, string blobName) where T : BlobBaseClient
        {
            BlobBaseClient blob;
            BlobContainerClient container = new(connectionString, containerName);
            Type targetType = typeof(T);

            blob = targetType switch {
                Type _ when targetType == typeof(BlobClient)        => container.GetBlobClient(blobName),
                Type _ when targetType == typeof(BlockBlobClient)   => container.GetBlockBlobClient(blobName),
                Type _ when targetType == typeof(PageBlobClient)    => container.GetPageBlobClient(blobName),
                Type _ when targetType == typeof(AppendBlobClient)  => container.GetAppendBlobClient(blobName),
                _ => new(connectionString, containerName, blobName)
            };

            return (T)blob;
        }
    }
}
