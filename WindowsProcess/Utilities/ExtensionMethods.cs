using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using Microsoft.Win32.SafeHandles;

namespace WindowsProcess.Utilities
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

        public static Dictionary<string, string> ToStringDictionary(this IDictionary dictionary)
        {
            Dictionary<string, string> typedDictionary = new Dictionary<string, string>();
            foreach (DictionaryEntry de in dictionary)
            {
                typedDictionary[(string)de.Key] = (string)de.Value;
            }

            return typedDictionary;
        }

        public static SafeUserTokenHandle LogonAndGetUserPrimaryToken(this NetworkCredential credential)
        {
            SafeUserTokenHandle userTokenHandle;

            if (!NativeMethods.RevertToSelf())
            {
                throw new Win32Exception();
            }

            IntPtr token = IntPtr.Zero;
            try
            {
                if (!NativeMethods.LogonUser(
                    credential.UserName,
                    credential.Domain,
                    credential.Password,
                    LogonType.LOGON32_LOGON_INTERACTIVE,
                    LogonProvider.LOGON32_PROVIDER_DEFAULT,
                    out token))
                {
                    throw new Win32Exception();
                }

                userTokenHandle = new SafeUserTokenHandle(token.DuplicateHandle());
            }
            finally
            {
                if (token != IntPtr.Zero)
                {
                    NativeMethods.CloseHandle(token);
                }
            }

            return userTokenHandle;
        }


    }
}
