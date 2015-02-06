using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NSubstitute;
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

        [Fact]
        public void WhenOutputStreamProvided_CanBeRead()
        {
            MemoryStream stream = new MemoryStream();

            WindowsProcessIO processIo = new WindowsProcessIO(stream, stream, null);

            processIo.Input.WriteLine("Input1");
            processIo.Input.WriteLine("Input2");
            processIo.Input.Flush();

            stream.Seek(0, SeekOrigin.Begin);
            Assert.Equal("Input1", processIo.Output.ReadLine());
            Assert.Equal("Input2", processIo.Output.ReadLine());
        }

        [Fact]
        public void WhenErrorStreamProvided_CanBeRead()
        {
            MemoryStream stream = new MemoryStream();

            WindowsProcessIO processIo = new WindowsProcessIO(stream, null, stream);

            processIo.Input.WriteLine("Input1");
            processIo.Input.WriteLine("Input2");
            processIo.Input.Flush();

            stream.Seek(0, SeekOrigin.Begin);
            Assert.Equal("Input1", processIo.Error.ReadLine());
            Assert.Equal("Input2", processIo.Error.ReadLine());
        }

        [Fact]
        public void WhenDisposed_DisposesStreams()
        {
            var stream = Substitute.ForPartsOf<MemoryStream>();
            WindowsProcessIO processIo = new WindowsProcessIO(stream, stream, stream);
            processIo.Dispose();

            stream.Received(3).Dispose();
        }
    }
}
