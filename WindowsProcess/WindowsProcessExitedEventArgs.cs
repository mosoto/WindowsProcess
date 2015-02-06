using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsProcess
{
    public class WindowsProcessExitedEventArgs : EventArgs
    {
        public int ExitCode { get; set; }
    }
}
