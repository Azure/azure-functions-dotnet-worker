// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

﻿using System;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker.Extensions.Storage
{
    public sealed class BlobOutputAttribute : OutputBindingAttribute
    {
        private readonly string _blobPath;

        /// <summary>Initializes a new instance of the <see cref="BlobOutputAttribute"/> class.</summary>
        /// <param name="name">The name of the property to which to bind</param>
        /// <param name="blobPath">The path of the blob to which to bind.</param>
        public BlobOutputAttribute(string blobPath)
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
        /// Gets or sets the app setting name that contains the Azure Storage connection string.
        /// </summary>
        public string? Connection { get; set; }
    }
}
