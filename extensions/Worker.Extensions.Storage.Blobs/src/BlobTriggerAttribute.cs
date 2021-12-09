// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

﻿using System;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker
{
    public sealed class BlobTriggerAttribute : TriggerBindingAttribute
    {
        private readonly string _blobPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlobTriggerAttribute"/> class.
        /// </summary>
        /// <param name="blobPath">The path of the blob to which to bind.</param>
        /// <remarks>
        /// The blob portion of the path can contain tokens in curly braces to indicate a pattern to match. The matched
        /// name can be used in other binding attributes to define the output name of a Job function.
        /// </remarks>
        public BlobTriggerAttribute(string blobPath)
        {
            _blobPath = blobPath;
        }

        /// <summary>Gets the path of the blob to which to bind.</summary>
        /// <remarks>
        /// The blob portion of the path can contain tokens in curly braces to indicate a pattern to match. The matched
        /// name can be used in other binding attributes to define the output name of a Job function.
        /// </remarks>
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
