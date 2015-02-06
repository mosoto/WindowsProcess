using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace WindowsProcess
{
    public sealed class SafeProcessHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeProcessHandle() : base(true) { }

        public SafeProcessHandle(IntPtr handle)
            : base(true)
        {
            SetHandle(handle);
        }

        override protected bool ReleaseHandle()
        {
            return NativeMethods.CloseHandle(handle);
        }
    }
}
