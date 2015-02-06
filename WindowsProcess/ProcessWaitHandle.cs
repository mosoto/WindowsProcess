using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace WindowsProcess
{
    internal class ProcessWaitHandle : WaitHandle
    {
        internal ProcessWaitHandle(SafeProcessHandle processHandle)
        {
            this.SafeWaitHandle = new SafeWaitHandle(processHandle.DangerousGetHandle().DuplicateHandle(), true);
        }
    }
}
