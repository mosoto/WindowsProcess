using System.Runtime.InteropServices;

namespace WindowsProcess
{
    internal class NullHandleWindowsProcessIO : IWindowsProcessIO
    {
        public SafeHandle StdInputHandle
        {
            get { return null; }
        }

        public SafeHandle StdOutputHandle
        {
            get { return null; }
        }

        public SafeHandle StdErrorHandle
        {
            get { return null; }
        }

        public void Start()
        {
        }
    }
}
