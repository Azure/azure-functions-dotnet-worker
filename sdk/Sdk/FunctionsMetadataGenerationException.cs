// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Functions.Worker.Sdk
{
    [Serializable]
    internal class FunctionsMetadataGenerationException: Exception
    {
        public FunctionsMetadataGenerationException() { }

        internal FunctionsMetadataGenerationException(string message): base(message) { }

        internal FunctionsMetadataGenerationException(string message, Exception innerException) : base(message, innerException) { }
        
        protected FunctionsMetadataGenerationException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}
