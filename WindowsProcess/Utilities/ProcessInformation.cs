using System;
using WindowsProcess.Utilities;

namespace WindowsProcess.Utilities
{
    internal class ProcessInformation : IDisposable
    {
        public ProcessInformation(PROCESS_INFORMATION procInfo)
        {
            //
            // Marshalling doesn't support receiving SafeHandle in properties
            // so we are forced to receive IntPtr and then converting them.
            //
            ProcessHandle = new SafeProcessHandle(procInfo.hProcess);
            ThreadHandle = new SafeProcessHandle(procInfo.hThread);
            ProcessId = procInfo.dwProcessId;
            ThreadId = procInfo.dwThreadId;
        }

        public SafeProcessHandle ProcessHandle { get; private set; }

        public SafeProcessHandle ThreadHandle { get; private set; }

        public int ProcessId { get; private set; }

        public int ThreadId { get; private set; }

        public void Dispose()
        {
            if (ProcessHandle != null)
            {
                ProcessHandle.Dispose();
                ProcessHandle = null;
            }

            if (ThreadHandle != null)
            {
                ThreadHandle.Dispose();
                ThreadHandle = null;
            }
        }
    }
}
