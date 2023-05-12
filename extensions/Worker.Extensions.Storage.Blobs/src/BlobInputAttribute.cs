// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker
{
    [AllowConverterFallback(false)]
    [InputConverter(typeof(BlobStorageConverter))]
    public sealed class BlobInputAttribute : InputBindingAttribute
    {
        private readonly string _blobPath;

        private bool _isBatched = false;

        /// <summary>Initializes a new instance of the <see cref="BlobInputAttribute"/> class.</summary>
        /// <param name="blobPath">The path of the blob to which to bind.</param>
        public BlobInputAttribute(string blobPath)
        {
            _blobPath = blobPath;
        }

        /// <summary>
        /// Gets the path of the blob to which to bind.
        /// </summary>
        public string BlobPath
        {
            get { return _blobPath; }
        }

        /// <summary>
        /// Gets or sets the configuration to enable batch processing of blobs. Default value is "false".
        /// </summary>
        [DefaultValue(false)]
        public bool IsBatched
        {
            get => _isBatched;
            set => _isBatched = value;
        }

        /// <summary>
        /// Gets or sets the app setting name that contains the Azure Storage connection string.
        /// </summary>
        public string? Connection { get; set; }
    }
}
