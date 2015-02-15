using System;
using System.ComponentModel;
using System.Threading;
using WindowsProcess.Utilities;

// ReSharper disable InconsistentNaming

namespace WindowsProcess
{
    // ReSharper disable once InconsistentNaming

    public interface IWindowsProcess : IDisposable
    {
        WindowsProcessStartInfo StartInfo { get; }
        int Id { get; }
        int? ExitCode { get; }
        bool HasExited { get; }

        event EventHandler<WindowsProcessExitedEventArgs> Exited;

        bool WaitForExit(int milliseconds = Timeout.Infinite);

        /// <summary>
        /// Terminates the process.
        /// </summary>
        /// <param name="exitCode">The exit code used by the terminated process</param>
        void Kill(int exitCode = -1);

        void Start();
    }

    public partial class WindowsProcess : IWindowsProcess
    {
        private readonly ProcessInformation _processInfo;
        private readonly object _syncObject = new object();

        private bool _disposed = false;
        private int? _exitCode;
        private WaitHandle _processExitWaitHandle;
        private RegisteredWaitHandle _processExitRegisteredWaitHandle;


        public WindowsProcessStartInfo StartInfo { get; private set; }

        public int Id
        {
            get
            {
                ThrowIfDisposed();
                return _processInfo.ProcessId;
            }
        }

        public int? ExitCode
        {
            get
            {
                ThrowIfDisposed();
                return _exitCode;
            }
        }

        public bool HasExited
        {
            get
            {
                ThrowIfDisposed();
                return _exitCode.HasValue;
            }
        }

        public event EventHandler<WindowsProcessExitedEventArgs> Exited;

        private WindowsProcess(ProcessInformation processInfo, WindowsProcessStartInfo startInfo)
        {
            _processInfo = processInfo;
            this.StartInfo = startInfo;

            StartWatchingForExit();
        }

        // TODO: Waiting an infinite time for exit on a process with redirected input is a recipe for deadlock.  Can we do something to warn the user?
        public bool WaitForExit(int milliseconds = Timeout.Infinite)
        {
            ThrowIfDisposed();

            bool exited = false;

            using (var processWaitHandle = new ProcessWaitHandle(_processInfo.ProcessHandle))
            {
                if (processWaitHandle.WaitOne(milliseconds, false))
                {
                    exited = true;

                    SetExitCode();
                }
            }

            return exited;
        }

        /// <summary>
        /// If suspended, resumes the process.
        /// </summary>
        public void Start()
        {
            bool processStillSuspended = true;
            while(processStillSuspended)
            {
                uint retValue = NativeMethods.ResumeThread(_processInfo.ThreadHandle);
                // For an explanation of the following see: https://msdn.microsoft.com/en-us/library/windows/desktop/ms685086(v=vs.85).aspx
                // The ResumeThread function checks the suspend count of the subject thread. If the suspend count is zero, the
                // thread is not currently suspended. Otherwise, the subject thread's suspend count is decremented. If the
                // resulting value is zero, then the execution of the subject thread is resumed.
                //
                // If the return value is zero, the specified thread was not suspended. If the return value is 1, the specified
                // thread was suspended but was restarted. If the return value is greater than 1, the specified thread is still suspended.
                processStillSuspended = retValue > 1;
            }
        }

        private void NativeResumeProcess()
        {
            
        }

        /// <summary>
        /// Terminates the process.
        /// </summary>
        /// <param name="exitCode">The exit code used by the terminated process</param>
        public void Kill(int exitCode = -1)
        {
            ThrowIfDisposed();

            if (!NativeMethods.TerminateProcess(_processInfo.ProcessHandle, exitCode))
                throw new Win32Exception(); 
        }

        public void Dispose()
        {
            StopWatchingForExit();

            _processInfo.Dispose();
            
            _disposed = true;
        }

        private void OnExited(int exitCode)
        {
            var exited = Exited;
            if (exited != null)
            {
                var args = new WindowsProcessExitedEventArgs { ExitCode = exitCode };
                exited(this, args);
            }
        }

        private void StartWatchingForExit()
        {
            lock (_syncObject)
            {
                _processExitWaitHandle = new ProcessWaitHandle(_processInfo.ProcessHandle);
                _processExitRegisteredWaitHandle = ThreadPool.RegisterWaitForSingleObject(
                    _processExitWaitHandle,
                    new WaitOrTimerCallback(ProcessExitedCallback), null, Timeout.Infinite, true);
            }
        }

        private void StopWatchingForExit()
        {
            lock (_syncObject)
            {
                if (_processExitRegisteredWaitHandle != null)
                {
                    _processExitRegisteredWaitHandle.Unregister(null);
                    _processExitRegisteredWaitHandle = null;
                }

                if (_processExitWaitHandle != null)
                {
                    _processExitWaitHandle.Close();
                    _processExitWaitHandle = null;
                }
            }
        }

        private void ProcessExitedCallback(object context, bool wasSignaled)
        {
            StopWatchingForExit();
            SetExitCode();
            OnExited(_exitCode.Value);
        }

        private void SetExitCode()
        {
            if (!_exitCode.HasValue)
            {
                int exitCode;
                NativeMethods.GetExitCodeProcess(_processInfo.ProcessHandle, out exitCode);
                _exitCode = exitCode;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new InvalidOperationException("This instance has already been disposed.");
        }
    }
}
