using System;
using Microsoft.Win32.SafeHandles;

namespace WindowsProcess.Utilities
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
