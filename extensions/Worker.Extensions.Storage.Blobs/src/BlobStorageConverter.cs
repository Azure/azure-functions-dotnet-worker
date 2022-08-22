// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Runtime;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Azure.Functions.Worker.Converters;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    /// <summary>
    /// Converter to bind Blob Storage type parameters.
    /// </summary>
    internal class BlobStorageConverter : IInputConverter
    {
        public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            BlobBaseClient? client = null;
            switch(context.TargetType)
            {
                case Type _ when context.TargetType == typeof(BlobBaseClient):
                    client = new BlobBaseClient("connection_string", "container_name", "blob_name");
                    break;

                case Type _ when context.TargetType == typeof(BlobClient):
                    client = new BlobClient("connection_string", "container_name", "blob_name");
                    break;

                case Type _ when context.TargetType == typeof(AppendBlobClient):
                    client = new AppendBlobClient("connection_string", "container_name", "blob_name");
                    break;

                case Type _ when context.TargetType == typeof(BlockBlobClient):
                    client= new BlockBlobClient("connection_string", "container_name", "blob_name");
                    break;

                case Type _ when context.TargetType == typeof(PageBlobClient):
                    client = new PageBlobClient("connection_string", "container_name", "blob_name");
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
