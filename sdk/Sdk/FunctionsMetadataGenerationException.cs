using System;

namespace Microsoft.Azure.Functions.Worker.Sdk
{
    internal class FunctionsMetadataGenerationException: Exception
    {
        internal FunctionsMetadataGenerationException(string message): base(message) { }

        internal FunctionsMetadataGenerationException(string message, Exception innerException) : base(message, innerException) { }

    }
}
