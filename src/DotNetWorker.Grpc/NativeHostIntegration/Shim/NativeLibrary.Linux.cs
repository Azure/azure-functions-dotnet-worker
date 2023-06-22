// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Azure.Functions.Worker.Grpc.NativeHostIntegration
{
    /// <summary>
    /// NativeLibrary.GetMainProgramHandle is only available from NET7.
    /// This shim calls the native API on Linux to get the main program handle
    /// </summary>
    internal class NativeLibraryLinux
    {
        //  Value 1 loads the library lazily, resolving symbols only as they are used
        private const int RTLD_LAZY = 1;

        [DllImport("libdl.so", CharSet = CharSet.Auto)]
        private static extern IntPtr dlerror();

        [DllImport("libdl.so", CharSet = CharSet.Auto)]
        private static extern IntPtr dlclose(nint handle);

        [DllImport("libdl.so", CharSet = CharSet.Auto)]
        private static extern IntPtr dlopen(string filename, int flags);

        internal static IntPtr GetMainProgramHandle()
        {
#pragma warning disable CS8625 // Passing null will return main program handle.
            var handle = dlopen(filename: null, RTLD_LAZY);
#pragma warning restore CS8625

            if (handle == IntPtr.Zero)
            {
                var error = Marshal.PtrToStringAnsi(dlerror());
                throw new InvalidOperationException($"Failed to get main program handle.{error}");
            }

            var result = dlclose(handle);
            if (result != IntPtr.Zero)
            {
                var error = Marshal.PtrToStringAnsi(dlerror());
                throw new InvalidOperationException($"Failed to close main program handle: {error}");
            }

            return handle;
        }
    }
}
