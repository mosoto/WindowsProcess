using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

// ReSharper disable InconsistentNaming

namespace WindowsProcess
{
    public interface IWindowsProcessIO
    {
        /// <summary>
        /// A handle to a pipe that can be written to to send messages to the child process.
        /// </summary>
        SafeHandle StdInputHandle { get; }
        /// <summary>
        /// A handle to a read pipe the child process can use to write output.
        /// </summary>
        SafeHandle StdOutputHandle { get; }
        /// <summary>
        /// A handle to a read pipe the child process can use to write error.
        /// </summary>
        SafeHandle StdErrorHandle { get; }

        /// <summary>
        /// Called by the process factory once the process is ready to start reading/writing.
        /// </summary>
        void Start();
    }
}
