using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace WindowsProcess.Tests
{
    public class WindowsProcessTests
    {
        WindowsProcessStartInfo StartInfo { get; set; }

        public WindowsProcessTests()
        {
            StartInfo = new WindowsProcessStartInfo();
        }

        public class Create : WindowsProcessTests
        {
            [Fact]
            public void WithNullFileName_Throws()
            {
                Action action = () => WindowsProcess.Create(StartInfo);
                Assert.Throws<ArgumentException>(action);
            }

            [Fact]
            public void WithFileNameAndArguments_StartsProcess()
            {
                StartInfo.FileName = "cmd.exe";
                StartInfo.Arguments = "/C";

                var process = WindowsProcess.Create(StartInfo);

                Assert.NotNull(process);
            }
        }
    }
}
