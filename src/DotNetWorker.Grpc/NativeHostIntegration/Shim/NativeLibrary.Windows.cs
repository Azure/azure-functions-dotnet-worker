// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Azure.Functions.Worker.Grpc.NativeHostIntegration
{
    /// <summary>
    /// NativeLibrary.GetMainProgramHandle is only available from NET7.
    /// This shim calls the native API on Windows to get the main program handle
    /// </summary>
    internal class NativeLibraryWindows
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        internal static IntPtr GetMainProgramHandle()
        {
#pragma warning disable CS8625 // Passing null will return main program handle.
            return GetModuleHandle(lpModuleName: null);
#pragma warning restore CS8625
        }
    }
}
