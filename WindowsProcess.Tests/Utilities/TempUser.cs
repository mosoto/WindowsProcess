using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WindowsProcess.Tests.Utilities
{
    class TempUser : IDisposable
    {
        public TempUser(NetworkCredential credential)
        {
            this.Credential = credential;
        }

        ~TempUser()
        {
            Dispose(false);
        }

        public NetworkCredential Credential { get; private set; }

        public void Dispose()
        {
            Dispose(true);
        }

        public void Dispose(bool disposing)
        {
            DeleteUser();

            if (disposing)
            {
                GC.SuppressFinalize(this);
            }
        }

        private void DeleteUser()
        {
            UserFactory.DeleteUser(this.Credential.UserName);
        }
    }
}
