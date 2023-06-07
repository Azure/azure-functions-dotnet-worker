// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Runtime.InteropServices;

namespace FunctionsNetHost
{
    internal class NetHost
    {
        [DllImport("nethost", CharSet = CharSet.Auto)]
        private static extern int get_hostfxr_path(
        [Out] char[] buffer,
        [In] ref int buffer_size,
        IntPtr reserved);

        internal static string GetHostFxrPath()
        {
            char[] buffer = new char[500];
            int buffer_size = buffer.Length;

            int rc = get_hostfxr_path(buffer, ref buffer_size, IntPtr.Zero);

            if (rc != 0)
            {
                throw new InvalidOperationException("Failed to get the hostfxr path.");
            }

            return new string(buffer, 0, buffer_size - 1);
        }
    }
}
