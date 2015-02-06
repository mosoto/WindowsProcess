using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsProcess
{
    public class WindowsProcessStartInfo
    {
        public WindowsProcessStartInfo()
        {
            //AutoStart = true;
            //WorkingDirectory = Environment.CurrentDirectory;
            //RedirectStandardInput = false;
            //RedirectStandardOutput = false;
            //RedirectStandardError = false;
            //LoadUserProfile = false;
        }

        public string FileName { get; set; }
        public string Arguments { get; set; }

        public bool RedirectStandardInput { get; set; }
        public bool RedirectStandardOutput { get; set; }
        public bool RedirectStandardError { get; set; }

        public bool AutoStart { get; set; }

        //public bool AutoStart { get; set; }
        //public string WorkingDirectory { get; set; }

        //public bool LoadUserProfile { get; set; }
        //public IDictionary<string,string> Environment { get; set; }
    }
}
