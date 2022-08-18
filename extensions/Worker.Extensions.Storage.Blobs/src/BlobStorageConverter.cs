// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Worker.Converters
{
    /// <summary>
    /// Converter to bind Blob Storage type parameters.
    /// </summary>
    internal class BlobStorageConverter : IInputConverter
    {
        public ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            switch(context.TargetType)
            {
                case BlobClient:
                    // hydrate blob client
                    return new ValueTask<ConversionResult>(ConversionResult.Success(blobClient));
                case BlobBaseClient:
                    // hydrate blob client
                    return new ValueTask<ConversionResult>(ConversionResult.Success(blobBaseClient));
                case AppendBlobClient:
                    // hydrate blob client
                    return new ValueTask<ConversionResult>(ConversionResult.Success(appendBlobClient));
                case BlockBlobClient:
                    // hydrate blob client
                    return new ValueTask<ConversionResult>(ConversionResult.Success(blockBlobClient));
                case PageBlobClient:
                    // hydrate blob client
                    return new ValueTask<ConversionResult>(ConversionResult.Success(pageBlobClient));
                default:
                    break;
            }

            return new ValueTask<ConversionResult>(ConversionResult.Unhandled());
        }
    }
}
