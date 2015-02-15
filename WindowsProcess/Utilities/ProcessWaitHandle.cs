using System;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace WindowsProcess.Utilities
{
    internal class ProcessWaitHandle : WaitHandle
    {
        public ProcessWaitHandle(SafeProcessHandle processHandle)
        {
            SafeWaitHandle = new SafeWaitHandle(processHandle.DangerousGetHandle().DuplicateHandle(), true);
        }
    }
}
