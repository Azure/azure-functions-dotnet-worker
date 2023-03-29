// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Runtime.InteropServices;

namespace FunctionsNetHost.ManagedLoader.NativeHostIntegration
{
    // I think we can remove the same type we added to src/DotNetWorker.Grpc/NativeHostIntegration 
    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeHost
    {
        public IntPtr pNativeApplication;
    }
}
