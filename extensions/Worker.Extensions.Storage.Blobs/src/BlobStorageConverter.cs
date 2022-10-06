// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Azure.Functions.Worker.Core;
using Newtonsoft.Json;

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

            if (bindingData.Source is not "Microsoft.Azure.WebJobs.Extensions.Storage.Blobs")
            {
                return new ValueTask<ConversionResult>(ConversionResult.Unhandled());
            }

            var content = JsonConvert.DeserializeObject<Dictionary<string, string>>(bindingData.Content);

            content.TryGetValue("Connection", out var connectionName);
            content.TryGetValue("BlobName", out var blobName);
            content.TryGetValue("ContainerName", out var containerName);

            if (string.IsNullOrEmpty(connectionName)
                || string.IsNullOrEmpty(blobName)
                || string.IsNullOrEmpty(containerName))
            {
                return new ValueTask<ConversionResult>(ConversionResult.Unhandled());
            }

            var connectionString = connectionName is null ? null : Environment.GetEnvironmentVariable(connectionName);
            object result = ToTargetType(context.TargetType, connectionString, containerName, blobName);

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
            Type _ when targetType == typeof(Byte[])            => GetBlobBinaryData(connectionString, containerName, blobName),
            Type _ when targetType == typeof(BlobClient)        => CreateBlobReference<BlobClient>(connectionString, containerName, blobName),
            Type _ when targetType == typeof(BlockBlobClient)   => CreateBlobReference<BlockBlobClient>(connectionString, containerName, blobName),
            Type _ when targetType == typeof(PageBlobClient)    => CreateBlobReference<PageBlobClient>(connectionString, containerName, blobName),
            Type _ when targetType == typeof(AppendBlobClient)  => CreateBlobReference<AppendBlobClient>(connectionString, containerName, blobName),
            _ => CreateTargetObject(targetType, connectionString, containerName, blobName)
        };

        private object CreateTargetObject(Type targetType, string connectionString, string containerName, string blobName)
        {
            var content = GetBlobString(connectionString, containerName, blobName);
            MethodInfo deserializeObjectMethod = GetType()
                                                .GetMethod(nameof(DeserializeTargetObject), BindingFlags.Instance | BindingFlags.NonPublic)
                                                .MakeGenericMethod(new Type[] { targetType });

            return deserializeObjectMethod.Invoke(this, new object[] { content });
        }

        private object? DeserializeTargetObject<T>(string content)
        {
            return JsonConvert.DeserializeObject<T>(content);
        }

        private string GetBlobString(string connectionString, string containerName, string blobName)
        {
            var client = CreateBlobReference<BlobClient>(connectionString, containerName, blobName);
            var download = client.DownloadContent();
            return download.Value.Content.ToString();
        }

        private Byte[] GetBlobBinaryData(string connectionString, string containerName, string blobName)
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
            Type targetType = typeof(T);
            BlobContainerClient container = new(connectionString, containerName);

            BlobBaseClient blob = targetType switch {
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
