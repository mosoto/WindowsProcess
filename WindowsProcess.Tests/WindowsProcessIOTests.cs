using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace WindowsProcess.Tests
{
    public class WindowsProcessIOTests
    {
        [Fact]
        public void WhenReadingNullStreams_Throws()
        {
            WindowsProcessIO processIo = new WindowsProcessIO(null, null, null);

            Assert.Throws<InvalidOperationException>(() => processIo.Input.WriteLine("foo"));
            Assert.Throws<InvalidOperationException>(() => processIo.Output.ReadLine());
            Assert.Throws<InvalidOperationException>(() => processIo.Error.ReadLine());
        }
    }
}
