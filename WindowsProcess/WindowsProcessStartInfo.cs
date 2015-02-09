using System;
using System.Collections.Generic;
using System.Linq;
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
        
        
        //public bool LoadUserProfile { get; set; }
        //public string UserName { get;set; }
        //public SecureString Password { get;set; }
    }
}
