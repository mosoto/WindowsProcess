using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace WindowsProcess
{
    public static class ExtensionMethods
    {
        public static IntPtr DuplicateHandle(this IntPtr handle)
        {
            return MakeHandleDuplicate(handle);
        }

        public static SafeWaitHandle DuplicateHandle(this SafeWaitHandle handle)
        {
            IntPtr ptr = MakeHandleDuplicate(handle.DangerousGetHandle());
            return new SafeWaitHandle(ptr, true);
        }

        public static SafeFileHandle DuplicateHandle(this SafeFileHandle handle)
        {
            IntPtr ptr = MakeHandleDuplicate(handle.DangerousGetHandle());
            return new SafeFileHandle(ptr, true);
        }

        private static IntPtr MakeHandleDuplicate(IntPtr sourceHandle)
        {
            IntPtr targetHandle;
            using (SafeWaitHandle currentProcHandle = NativeMethods.GetCurrentProcess())
            {
                if (!NativeMethods.DuplicateHandle(
                    currentProcHandle.DangerousGetHandle(),
                    sourceHandle,
                    currentProcHandle.DangerousGetHandle(),
                    out targetHandle,
                    0 /*  Ignored if asking for duplicate same access */,
                    false,
                    DuplicateHandleOptions.DUPLICATE_SAME_ACCESS
                    ))
                {
                    throw new Win32Exception();
                }
            }

            return targetHandle;
        }

    }
}
