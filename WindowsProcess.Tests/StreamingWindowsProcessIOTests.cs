using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using NSubstitute;
using Xunit;

namespace WindowsProcess.Tests
{
    public class StreamingWindowsProcessIOTests
    {
        [Fact]
        public void WhenReadingUnRedirectedStream_Throws()
        {
            var processIo = new StreamingWindowsProcessIO(false, false, false);

            Assert.Throws<InvalidOperationException>(() => processIo.InputStream.WriteLine());
            Assert.Throws<InvalidOperationException>(() => processIo.OutputStream.ReadLine());
            Assert.Throws<InvalidOperationException>(() => processIo.ErrorStream.ReadLine());
        }

        [Fact]
        public void WhenInputStreamRedirected_CanBeWritten()
        {
            var processIo = new StreamingWindowsProcessIO(true, false, false);

            AnonymousPipeClientStream client = new AnonymousPipeClientStream(PipeDirection.In, (SafePipeHandle)processIo.StdInputHandle);
            processIo.InputStream.WriteLine("Input1");
            processIo.InputStream.WriteLine("Input2");
            processIo.InputStream.Flush();
            
            using (StreamReader reader = new StreamReader(client))
            {
                Assert.Equal("Input1", reader.ReadLine());
                Assert.Equal("Input2", reader.ReadLine());
            }
        }

        [Fact]
        public void WhenOutputStreamRedirected_CanBeRead()
        {
            var processIo = new StreamingWindowsProcessIO(false, true, false);
            
            AnonymousPipeClientStream client = new AnonymousPipeClientStream(PipeDirection.Out, (SafePipeHandle)processIo.StdOutputHandle);
            using (StreamWriter writer = new StreamWriter(client))
            {
                writer.WriteLine("Input1");
                writer.WriteLine("Input2");
                writer.Flush();
            }

            Assert.Equal("Input1", processIo.OutputStream.ReadLine());
            Assert.Equal("Input2", processIo.OutputStream.ReadLine());
        }

        [Fact]
        public void WhenErrorStreamProvided_CanBeRead()
        {
            var processIo = new StreamingWindowsProcessIO(false, false, true);

            AnonymousPipeClientStream client = new AnonymousPipeClientStream(PipeDirection.Out, (SafePipeHandle)processIo.StdErrorHandle);
            using (StreamWriter writer = new StreamWriter(client))
            {
                writer.WriteLine("Input1");
                writer.WriteLine("Input2");
                writer.Flush();
            }

            Assert.Equal("Input1", processIo.ErrorStream.ReadLine());
            Assert.Equal("Input2", processIo.ErrorStream.ReadLine());
        }

        [Fact]
        public void WhenDisposed_DisposesStreams()
        {
            var processIo = new StreamingWindowsProcessIO(true, true, true);
            processIo.Dispose();

            Assert.Throws<ObjectDisposedException>(() => processIo.InputStream.WriteLine());
            Assert.Throws<ObjectDisposedException>(() => processIo.OutputStream.ReadToEnd());
            Assert.Throws<ObjectDisposedException>(() => processIo.ErrorStream.ReadToEnd());
        }

        [Fact]
        public void WhenStarted_ChildHandlesDisposed()
        {
            var processIo = new StreamingWindowsProcessIO(true, true, true);
            processIo.Start();

            Assert.True(processIo.StdInputHandle.IsClosed);
            Assert.True(processIo.StdOutputHandle.IsClosed);
            Assert.True(processIo.StdErrorHandle.IsClosed);
        }
    }
}
