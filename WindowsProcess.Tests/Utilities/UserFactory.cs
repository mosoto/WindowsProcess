using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using WindowsProcess.Tests.Utilities;

namespace WindowsProcess.Tests
{
    class UserFactory
    {
        const int NERR_Success = 0;
        [DllImport("netapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int NetUserDel(string serverName, string userName);

        public static TempUser CreateTempUser()
        {
            TempUser user = null;

            using (var context = new PrincipalContext(ContextType.Machine))
            {
                string userName = CreateRandomString();
                string password = CreateRandomString();

                using (UserPrincipal userPrincipal = new UserPrincipal(context))
                {
                    userPrincipal.SamAccountName = userName;
                    userPrincipal.SetPassword(password);
                    userPrincipal.Enabled = true;
                    userPrincipal.Save();
                }

                var credential = new NetworkCredential(userName, password);
                user = new TempUser(credential);
            }

            return user;
        }

        public static void DeleteUser(string userName)
        {
            var result = NetUserDel(null, userName);

            if (result != NERR_Success)
            {
                throw new InvalidOperationException(
                    string.Format("Failed to delete user {0} with the error code: {1}", userName, result));
            }
        }

        private static string CreateRandomString()
        {
            return Guid.NewGuid().ToString("N").Substring(0, 20);
        }
    }
}
