using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace WindowsProcess
{
    public class WindowsProcessStartInfo
    {
        public string FileName { get; set; }
        public string Arguments { get; set; }
        public string WorkingDirectory { get; set; }
        public IWindowsProcessIO IO { get; set; }
        public bool AutoStart { get; set; }
        public IDictionary<string, string> Environment { get; set; }
        public NetworkCredential Credential { get; set; }
        public bool LoadUserProfile { get; set; }
        public bool CreateNoWindow { get; set; }
    }
}
