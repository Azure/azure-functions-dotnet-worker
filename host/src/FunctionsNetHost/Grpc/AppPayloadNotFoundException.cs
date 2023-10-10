// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker
{
    public class AppPayloadNotFoundException : Exception
    {
        public AppPayloadNotFoundException() { }

        public AppPayloadNotFoundException(string message) : base(message) { }
    }
}
