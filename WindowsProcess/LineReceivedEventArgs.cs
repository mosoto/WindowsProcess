using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsProcess
{
    public class LineReceivedEventArgs : EventArgs
    {
        public LineReceivedEventArgs(string line)
        {
            Line = line;
        }

        public string Line { get; set; }
    }
}
