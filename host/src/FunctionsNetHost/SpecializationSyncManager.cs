// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace FunctionsNetHost
{
    internal static class SpecializationSyncManager
    {
        internal static readonly EventWaitHandle WaitHandle = new(false, EventResetMode.ManualReset,
            "AzureFunctionsNetHostSpecializationWaitHandle");
    }
}
