// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.Functions.Worker.Extensions.ServiceBus
{
    internal static class Constants
    {
        internal const string BinaryContentType = "application/octet-stream";

        internal const string BindingSource = "AzureServiceBusReceivedMessage";

        internal const string SessionId = "SessionId";

        internal const string SessionIdArray = "SessionIdArray";

        internal const string SessionActions = "SessionActions";

        internal const string SessionLockedUntil = "SessionLockedUntil";
    }
}
