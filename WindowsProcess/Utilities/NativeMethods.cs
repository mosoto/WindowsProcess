using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
// ReSharper disable InconsistentNaming

namespace WindowsProcess.Utilities
{
    using DWORD  = UInt32;
    using LPBYTE = IntPtr;
    using LPVOID = IntPtr;
    using HANDLE = IntPtr;

    public static class NativeMethods
    {
        public const string Kernel32 = "kernel32.dll";
        public const string Advapi32 = "advapi32.dll";

        public static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        [DllImport(Kernel32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport(Kernel32, SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CreateProcess(
           string lpApplicationName,
           string lpCommandLine,
           SECURITY_ATTRIBUTES lpProcessAttributes,
           SECURITY_ATTRIBUTES lpThreadAttributes,
           bool bInheritHandles,
           ProcesCreationFlags dwCreationFlags,
           LPVOID lpEnvironment,
           string lpCurrentDirectory,
           STARTUPINFO lpStartupInfo,
           out PROCESS_INFORMATION lpProcessInformation);

        [DllImport(Advapi32, SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CreateProcessWithLogonW(
            String userName,
            String domain,
            IntPtr password,
            LogonFlags logonFlags,
            [MarshalAs(UnmanagedType.LPTStr)] 
            String applicationName,
            String commandLine,
            ProcesCreationFlags creationFlags,
            LPVOID environment,
            [MarshalAs(UnmanagedType.LPTStr)] 
            String currentDirectory,
            STARTUPINFO startupInfo,
            out PROCESS_INFORMATION processInformation);


        [DllImport(Kernel32, CharSet = CharSet.Ansi, SetLastError = true, BestFitMapping = false)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DuplicateHandle(
            IntPtr hSourceProcessHandle,
            IntPtr hSourceHandle,
            IntPtr hTargetProcess,
            out IntPtr targetHandle,
            DWORD dwDesiredAccess,
            bool bInheritHandle,
            DuplicateHandleOptions dwOptions
        );

        [DllImport(Kernel32, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern SafeWaitHandle GetCurrentProcess();

        [DllImport(Kernel32, CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool TerminateProcess(SafeProcessHandle processHandle, int exitCode);

        [DllImport(Kernel32, CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetExitCodeProcess(SafeProcessHandle processHandle, out int exitCode);

        [DllImport(Kernel32, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern IntPtr GetStdHandle(StandardHandle whichHandle);

        [DllImport(Kernel32, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern uint ResumeThread(SafeHandle hThread);

        [DllImport(Advapi32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool LogonUser(
            String lpszUserName,
            String lpszDomain,
            String lpszPassword,
            LogonType dwLogonType,
            LogonProvider dwLogonProvider,
            out IntPtr phToken);

        [DllImport(Advapi32, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool RevertToSelf();
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public class STARTUPINFO : IDisposable
    {
        public Int32 cb;
        public string lpReserved;
        public string lpDesktop;
        public string lpTitle;
        public Int32 dwX;
        public Int32 dwY;
        public Int32 dwXSize;
        public Int32 dwYSize;
        public Int32 dwXCountChars;
        public Int32 dwYCountChars;
        public Int32 dwFillAttribute;
        public StartInfoFlags dwFlags;
        public Int16 wShowWindow;
        public Int16 cbReserved2;
        public LPBYTE lpReserved2;
        public SafeHandle hStdInput = new SafePipeHandle(IntPtr.Zero, false);
        public SafeHandle hStdOutput = new SafePipeHandle(IntPtr.Zero, false);
        public SafeHandle hStdError = new SafePipeHandle(IntPtr.Zero, false);

        public STARTUPINFO()
        {
            cb = Marshal.SizeOf(this);
        }

        public void Dispose()
        {
            if (hStdInput != null && !hStdInput.IsInvalid)
            {
                hStdInput.Close();
                hStdInput = null;
            }

            if (hStdOutput != null && !hStdOutput.IsInvalid)
            {
                hStdOutput.Close();
                hStdOutput = null;
            }

            if (hStdError != null && !hStdError.IsInvalid)
            {
                hStdError.Close();
                hStdError = null;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PROCESS_INFORMATION
    {
        public HANDLE hProcess;
        public HANDLE hThread;
        public int dwProcessId;
        public int dwThreadId;
    }

    [StructLayout(LayoutKind.Sequential)]
    public class SECURITY_ATTRIBUTES
    {
        public int length;
        public LPVOID lpSecurityDescriptor;
        public bool bInheritHandle;
    }

    /// <summary>
    /// https://msdn.microsoft.com/en-us/library/windows/desktop/ms684863(v=vs.85).aspx
    /// </summary>
    [Flags]
    public enum ProcesCreationFlags : uint
    {
        NONE = 0x00000000,
        DEBUG_PROCESS = 0x00000001,
        DEBUG_ONLY_THIS_PROCESS = 0x00000002,
        CREATE_SUSPENDED = 0x00000004,
        DETACHED_PROCESS = 0x00000008,
        CREATE_NEW_CONSOLE = 0x00000010,
        CREATE_NEW_PROCESS_GROUP = 0x00000200,
        CREATE_UNICODE_ENVIRONMENT = 0x00000400,
        CREATE_SEPARATE_WOW_VDM = 0x00000800,
        CREATE_SHARED_WOW_VDM = 0x00001000,
        INHERIT_PARENT_AFFINITY = 0x00010000,
        CREATE_PROTECTED_PROCESS = 0x00040000,
        EXTENDED_STARTUPINFO_PRESENT = 0x00080000,
        CREATE_BREAKAWAY_FROM_JOB = 0x01000000,
        CREATE_PRESERVE_CODE_AUTHZ_LEVEL = 0x02000000,
        CREATE_DEFAULT_ERROR_MODE = 0x04000000,
        CREATE_NO_WINDOW = 0x08000000
    }

    /// <summary>
    /// https://msdn.microsoft.com/en-us/library/windows/desktop/ms686331(v=vs.85).aspx
    /// </summary>
    [Flags]
    public enum StartInfoFlags : uint
    {
        STARTF_USESHOWWINDOW = 0x00000001,
        STARTF_USESIZE = 0x00000002,
        STARTF_USEPOSITION = 0x00000004,
        STARTF_USECOUNTCHARS = 0x00000008,
        STARTF_USEFILLATTRIBUTE = 0x00000010,
        STARTF_RUNFULLSCREEN = 0x00000020,
        STARTF_FORCEONFEEDBACK = 0x00000040,
        STARTF_FORCEOFFFEEDBACK = 0x00000080,
        STARTF_USESTDHANDLES = 0x00000100,
        STARTF_USEHOTKEY = 0x00000200,
        STARTF_TITLEISLINKNAME = 0x00000800,
        STARTF_TITLEISAPPID = 0x00001000,
        STARTF_PREVENTPINNING = 0x00002000,
    }

    /// <summary>
    /// https://msdn.microsoft.com/en-us/library/windows/desktop/ms724251%28v=vs.85%29.aspx
    /// </summary>
    [Flags]
    public enum DuplicateHandleOptions : uint
    {
        DUPLICATE_CLOSE_SOURCE = 0x00000001, 
        DUPLICATE_SAME_ACCESS  = 0x00000002, 
    }

    /// <summary>
    /// https://msdn.microsoft.com/en-us/library/windows/desktop/ms683231%28v=vs.85%29.aspx
    /// </summary>
    [Flags]
    public enum StandardHandle : int
    {
        STD_INPUT_HANDLE = -10,
        STD_OUTPUT_HANDLE = -11,
        STD_ERROR_HANDLE = -12,
    }

    [Flags]
    public enum LogonFlags : uint
    {
        NONE = 0,
        LOGON_WITH_PROFILE = 0x00000001,
        LOGON_NETCREDENTIALS_ONLY = 0x00000002
    }

    [Flags]
    public enum LogonType : uint
    {
        LOGON32_LOGON_INTERACTIVE = 2,
        LOGON32_LOGON_NETWORK = 3,
        LOGON32_LOGON_BATCH = 4,
        LOGON32_LOGON_SERVICE = 5,
        LOGON32_LOGON_UNLOCK = 7,
        LOGON32_LOGON_NETWORK_CLEARTEXT = 8,
        LOGON32_LOGON_NEW_CREDENTIALS = 9
    }

    [Flags]
    public enum LogonProvider : uint
    {
        LOGON32_PROVIDER_DEFAULT = 0,
        LOGON32_PROVIDER_WINNT35,
        LOGON32_PROVIDER_WINNT40,
        LOGON32_PROVIDER_WINNT50
    }
}
