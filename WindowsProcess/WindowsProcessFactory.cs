using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace WindowsProcess
{
    public partial class WindowsProcess
    {
        public static IWindowsProcess Create(WindowsProcessStartInfo startInfo)
        {
            if (startInfo == null)
            {
                throw new ArgumentNullException("startInfo");
            }

            if (String.IsNullOrWhiteSpace(startInfo.FileName))
            {
                throw new ArgumentException("startInfo.FileName");
            }

            string commandLine = BuildCommandLine(startInfo.FileName, startInfo.Arguments);
            ProcesCreationFlags creationFlags = ProcesCreationFlags.NONE;
            IntPtr environmentPtr = IntPtr.Zero;
            string workingDirectory = startInfo.WorkingDirectory;

            var startupInfo = new STARTUPINFO();

            IWindowsProcessIO ioHandles = startInfo.IO ?? new NullHandleWindowsProcessIO();
            startupInfo.hStdInput = ioHandles.StdInputHandle ?? GetStandardHandle(StandardHandle.STD_INPUT_HANDLE);
            startupInfo.hStdOutput = ioHandles.StdOutputHandle ?? GetStandardHandle(StandardHandle.STD_OUTPUT_HANDLE);
            startupInfo.hStdError = ioHandles.StdErrorHandle ?? GetStandardHandle(StandardHandle.STD_ERROR_HANDLE);

            bool anyStreamRedirected = ioHandles.StdInputHandle != null || 
                                       ioHandles.StdOutputHandle != null ||
                                       ioHandles.StdErrorHandle != null;

            if (anyStreamRedirected)
                // Set a flag to indicate that we are passing the child standard handles.
                startupInfo.dwFlags |= StartInfoFlags.STARTF_USESTDHANDLES;

            PROCESS_INFORMATION processInfo;
            bool retValue = NativeMethods.CreateProcess(
                null, // we don't need this since all the info is in commandLine 
                commandLine, // pointer to the command line string
                null, // pointer to process security attributes, we don't need to inheriat the handle 
                null, // pointer to thread security attributes 
                true, // handle inheritance flag
                creationFlags, // creation flags 
                environmentPtr, // pointer to new environment block
                workingDirectory, // pointer to current directory name
                startupInfo, // pointer to STARTUPINFO
                out processInfo // pointer to PROCESS_INFORMATION 
                );
            if (!retValue)
            {
                throw new Win32Exception();
            }

            ioHandles.Start();
            var pInfo = new ProcessInformation(processInfo);
            var process = new WindowsProcess(pInfo, startInfo);

            // TODO If AutoStart: process.Start();

            return process;
        }

        private static SafePipeHandle GetStandardHandle(StandardHandle handleType)
        {
            var stdHandle = NativeMethods.GetStdHandle(handleType);
            return new SafePipeHandle(stdHandle, false);
        }

        private static string BuildCommandLine(string executableFileName, string arguments)
        {
            // Construct a StringBuilder with the appropriate command line 
            // to pass to CreateProcess.  If the filename isn't already
            // in quotes, we quote it here.  This prevents some security
            // problems (it specifies exactly which part of the string
            // is the file to execute). 
            StringBuilder commandLine = new StringBuilder();
            string fileName = executableFileName.Trim();
            bool fileNameIsQuoted = (fileName.StartsWith("\"", StringComparison.Ordinal) && fileName.EndsWith("\"", StringComparison.Ordinal));
            if (!fileNameIsQuoted)
            {
                commandLine.Append("\"");
            }

            commandLine.Append(fileName);

            if (!fileNameIsQuoted)
            {
                commandLine.Append("\"");
            }

            if (!String.IsNullOrEmpty(arguments))
            {
                commandLine.Append(" ");
                commandLine.Append(arguments);
            }

            return commandLine.ToString();
        }
    }
}
