using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ManagedLoader
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeHost
    {
        public IntPtr pNativeApplication;
    }
}
