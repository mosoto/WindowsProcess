using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using WindowsProcess.Utilities;
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

            WindowsProcess process = null;
            GCHandle environmentHandle = new GCHandle();
            IntPtr password = IntPtr.Zero;

            try
            {
                // COMMAND LINE
                string commandLine = BuildCommandLine(startInfo.FileName, startInfo.Arguments);

                // CREATION FLAGS
                ProcesCreationFlags creationFlags = ProcesCreationFlags.CREATE_SUSPENDED;
                if (startInfo.CreateNoWindow)
                    creationFlags |= ProcesCreationFlags.CREATE_NO_WINDOW;

                // ENVIRONMENT
                IntPtr environmentPtr = IntPtr.Zero;
                if (startInfo.Environment != null)
                {
                    Encoding envEncoding = Environment.OSVersion.Platform == PlatformID.Win32NT
                        ? Encoding.Unicode
                        : Encoding.Default;

                    byte[] environmentBytes = EnvBlockToBytes(startInfo.Environment, envEncoding);
                    environmentHandle = GCHandle.Alloc(environmentBytes, GCHandleType.Pinned);
                    environmentPtr = environmentHandle.AddrOfPinnedObject();

                    if (envEncoding.Equals(Encoding.Unicode))
                    {
                        creationFlags |= ProcesCreationFlags.CREATE_UNICODE_ENVIRONMENT;
                    }
                }

                // WORKING DIR
                string workingDirectory = startInfo.WorkingDirectory;

                // STARTUP INFO
                var startupInfo = new STARTUPINFO();

                IWindowsProcessIO ioHandles = startInfo.IO ?? new NullHandleWindowsProcessIO();
                startupInfo.hStdInput = ioHandles.StdInputHandle ?? GetStandardHandle(StandardHandle.STD_INPUT_HANDLE);
                startupInfo.hStdOutput = ioHandles.StdOutputHandle ??
                                         GetStandardHandle(StandardHandle.STD_OUTPUT_HANDLE);
                startupInfo.hStdError = ioHandles.StdErrorHandle ?? GetStandardHandle(StandardHandle.STD_ERROR_HANDLE);

                bool anyStreamRedirected = ioHandles.StdInputHandle != null ||
                                           ioHandles.StdOutputHandle != null ||
                                           ioHandles.StdErrorHandle != null;

                if (anyStreamRedirected)
                    // Set a flag to indicate that we are passing the child standard handles.
                    startupInfo.dwFlags |= StartInfoFlags.STARTF_USESTDHANDLES;


                // START PROCESS
                PROCESS_INFORMATION processInfo;
                bool retValue = false;
                if (startInfo.Credential != null)
                {
                    string userName = startInfo.Credential.UserName;

                    password = startInfo.Credential.SecurePassword == null
                        ? Marshal.StringToCoTaskMemUni(string.Empty)
                        : Marshal.SecureStringToCoTaskMemUnicode(startInfo.Credential.SecurePassword);

                    string domain = startInfo.Credential.Domain;

                    LogonFlags logonFlags = LogonFlags.NONE;
                    if (startInfo.LoadUserProfile)
                    {
                        logonFlags |= LogonFlags.LOGON_WITH_PROFILE;
                    }

                    retValue = NativeMethods.CreateProcessWithLogonW(
                        userName,
                        domain,
                        password,
                        logonFlags,
                        null,
                        commandLine,
                        creationFlags,
                        environmentPtr,
                        workingDirectory,
                        startupInfo,
                        out processInfo
                        );
                }
                else
                {
                    retValue = NativeMethods.CreateProcess(
                        null, // we don't need this since all the info is in commandLine 
                        commandLine, // pointer to the command line string
                        null, // pointer to process security attributes 
                        null, // pointer to thread security attributes 
                        true, // handle inheritance flag
                        creationFlags, // creation flags 
                        environmentPtr, // pointer to new environment block
                        workingDirectory, // pointer to current directory name
                        startupInfo, // pointer to STARTUPINFO
                        out processInfo // pointer to PROCESS_INFORMATION 
                        );
                }

                if (!retValue)
                {
                    throw new Win32Exception();
                }

                if (processInfo.hProcess == IntPtr.Zero
                    || processInfo.hProcess == NativeMethods.INVALID_HANDLE_VALUE
                    || processInfo.hThread == IntPtr.Zero
                    || processInfo.hThread == NativeMethods.INVALID_HANDLE_VALUE)
                {
                    var errorCode = Marshal.GetLastWin32Error();
                    throw new Win32Exception(errorCode);
                }

                ioHandles.Start();
                var pInfo = new ProcessInformation(processInfo);
                process = new WindowsProcess(pInfo, startInfo);

                if (startInfo.AutoStart)
                    process.Start();
            }
            finally
            {
                if (environmentHandle.IsAllocated)
                {
                    environmentHandle.Free();
                }

                if (password != IntPtr.Zero)
                {
                    Marshal.ZeroFreeCoTaskMemUnicode(password);
                }
            }

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

        private static byte[] EnvBlockToBytes(IDictionary<string, string> envBlock, Encoding encoding)
        {
            var sortedPairs = envBlock.OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase).ToArray();

            StringBuilder buffer = new StringBuilder();
            foreach (var keyValuePair in sortedPairs)
            {
                buffer.Append(keyValuePair.Key);
                buffer.Append('=');
                buffer.Append(keyValuePair.Value);
                buffer.Append('\0');
            }
            // The end of the block is indicated by an extra null.
            buffer.Append('\0');

            var bytes = encoding.GetBytes(buffer.ToString());

            if (bytes.Length > UInt16.MaxValue)
            {
                throw new InvalidOperationException(string.Format("The environment block must fit in {0} bytes", UInt16.MaxValue));
            }

            return bytes;
        }
    }
}
