// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs.TypeConverters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Functions.Worker.Extensions.Tests.Blob
{
    /// <summary>
    /// Test helper that composes the per-type blob converters in the same order they are registered on
    /// <see cref="BlobInputAttribute"/> / <see cref="BlobTriggerAttribute"/> and dispatches like the worker
    /// runtime does (<c>DefaultInputConversionFeature</c>): each converter is tried in registration order and
    /// the first result whose status is not <see cref="ConversionStatus.Unhandled"/> wins. This lets the existing
    /// behavior-level tests exercise the entire conversion pipeline after the converter split.
    /// </summary>
    internal sealed class BlobConverterTestDispatcher
    {
        private readonly IReadOnlyList<IInputConverter> _converters;

        public BlobConverterTestDispatcher(
            IOptions<WorkerOptions> workerOptions,
            IOptionsMonitor<BlobStorageBindingOptions> blobOptions,
            ILoggerFactory loggerFactory)
        {
            _converters = new IInputConverter[]
            {
                new BlobContainerClientConverter(workerOptions, blobOptions, loggerFactory.CreateLogger<BlobContainerClientConverter>()),
                new BlobClientConverter(workerOptions, blobOptions, loggerFactory.CreateLogger<BlobClientConverter>()),
                new BlobStringConverter(workerOptions, blobOptions, loggerFactory.CreateLogger<BlobStringConverter>()),
                new BlobStreamConverter(workerOptions, blobOptions, loggerFactory.CreateLogger<BlobStreamConverter>()),
                new BlobByteArrayConverter(workerOptions, blobOptions, loggerFactory.CreateLogger<BlobByteArrayConverter>()),
                new BlobCollectionConverter(workerOptions, blobOptions, loggerFactory.CreateLogger<BlobCollectionConverter>()),
                new BlobPocoConverter(workerOptions, blobOptions, loggerFactory.CreateLogger<BlobPocoConverter>()),
            };
        }

        public async ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
        {
            foreach (var converter in _converters)
            {
                var result = await converter.ConvertAsync(context);
                if (result.Status != ConversionStatus.Unhandled)
                {
                    return result;
                }
            }

            return ConversionResult.Unhandled();
        }
    }
}
