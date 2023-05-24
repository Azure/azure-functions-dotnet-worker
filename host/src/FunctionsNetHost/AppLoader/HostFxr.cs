// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Runtime.InteropServices;

namespace FunctionsNetHost
{
    static partial class HostFxr
    {
        public unsafe struct hostfxr_initialize_parameters
        {
            public nint size;
            public char* host_path;
            public char* dotnet_root;
        };

        [LibraryImport("hostfxr", EntryPoint = "hostfxr_initialize_for_dotnet_command_line")]
        public unsafe static partial int Initialize(
                int argc,
                [MarshalAs(UnmanagedType.LPArray, ArraySubType = 
#if LINUX
    UnmanagedType.LPStr
#else
     UnmanagedType.LPWStr
#endif
            )] string[] argv,
                ref hostfxr_initialize_parameters parameters,
                out IntPtr host_context_handle
            );

        [LibraryImport("hostfxr", EntryPoint = "hostfxr_run_app")]
        public static partial int Run(IntPtr host_context_handle);

        [LibraryImport("hostfxr", EntryPoint = "hostfxr_set_runtime_property_value")]
        public static partial int SetAppContextData(IntPtr host_context_handle, [MarshalAs(
#if LINUX
    UnmanagedType.LPStr
#else
     UnmanagedType.LPWStr
#endif
            )] string name, [MarshalAs(
#if LINUX
    UnmanagedType.LPStr
#else
     UnmanagedType.LPWStr
#endif
            )] string value);

    }
}
