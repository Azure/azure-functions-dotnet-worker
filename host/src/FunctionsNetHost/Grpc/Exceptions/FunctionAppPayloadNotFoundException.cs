// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker
{
    /// <summary>
    /// The exception that is thrown when there is no function app payload found.
    /// </summary>
    public sealed class FunctionAppPayloadNotFoundException : Exception
    {
        public FunctionAppPayloadNotFoundException() { }

        public FunctionAppPayloadNotFoundException(string message) : base(message) { }
    }
}
