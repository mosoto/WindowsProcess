﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable InconsistentNaming

namespace WindowsProcess
{
    // ReSharper disable once InconsistentNaming

    public interface IWindowsProcess : IDisposable
    {
        int Id { get; }
        int? ExitCode { get; }
        bool HasExited { get; }
        IWindowsProcessIO IO { get; }

        event EventHandler<WindowsProcessExitedEventArgs> Exited;

        bool WaitForExit(int milliseconds = Timeout.Infinite);

        /// <summary>
        /// Terminates the process.
        /// </summary>
        /// <param name="exitCode">The exit code used by the terminated process</param>
        void Kill(int exitCode = -1);
    }

    public partial class WindowsProcess : IWindowsProcess
    {
        private readonly ProcessInformation _processInfo;
        private readonly object _syncObject = new object();

        private IWindowsProcessIO _processIO;
        private bool _disposed = false;
        private int? _exitCode;
        private WaitHandle _processExitWaitHandle;
        private RegisteredWaitHandle _processExitRegisteredWaitHandle;


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

        public IWindowsProcessIO IO
        {
            get
            {
                ThrowIfDisposed();
                return _processIO;
            }
        }

        public event EventHandler<WindowsProcessExitedEventArgs> Exited;

        private WindowsProcess(ProcessInformation processInfo, IWindowsProcessIO processIO)
        {
            _processInfo = processInfo;
            _processIO = processIO;

            StartWatchingForExit();
        }

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

            // We don't dispose the process IO because someone else
            // might be using them.
            _processIO = null;
            
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
