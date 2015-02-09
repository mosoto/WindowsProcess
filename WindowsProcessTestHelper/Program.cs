using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsProcessTestHelper
{
    public class Program
    {
        static void Main(string[] args)
        {
            //System.Diagnostics.Debugger.Launch();

            if (args.Length == 0)
                throw new ArgumentException("args should have a length of 1 or greater");

            HashSet<string> arguments = new HashSet<string>(args, StringComparer.OrdinalIgnoreCase);

            string val = Console.IsInputRedirected
                ? Console.In.ReadToEnd()
                : "VALUE";

            if (arguments.Contains("OUTPUT"))
            {
                Console.Out.Write(val);
            }

            if (arguments.Contains("ERROR"))
            {
                Console.Error.Write(val);                
            }
        }
    }
}
