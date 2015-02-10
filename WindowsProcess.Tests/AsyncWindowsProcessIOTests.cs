using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using NSubstitute;
using Xunit;

namespace WindowsProcess.Tests
{
    public class AsyncWindowsProcessIOTests
    {
        public AnonymousPipeServerStream InputPipe { get; set; }
        public AnonymousPipeServerStream OutputPipe { get; set; }
        public AnonymousPipeServerStream ErrorPipe { get; set; }

        public IStreamingWindowsProcessIO StreamingIO { get; set; }

        public AsyncWindowsProcessIOTests()
        {
            InputPipe = new AnonymousPipeServerStream(PipeDirection.In);
            OutputPipe = new AnonymousPipeServerStream(PipeDirection.Out);
            ErrorPipe = new AnonymousPipeServerStream(PipeDirection.Out);
            
            StreamingIO = Substitute.For<IStreamingWindowsProcessIO>();
            StreamingIO.Input.Returns(new StreamWriter(new AnonymousPipeClientStream(PipeDirection.Out, InputPipe.ClientSafePipeHandle)));
            StreamingIO.Output.Returns(new StreamReader(new AnonymousPipeClientStream(PipeDirection.In, OutputPipe.ClientSafePipeHandle)));
            StreamingIO.Error.Returns(new StreamReader(new AnonymousPipeClientStream(PipeDirection.In, ErrorPipe.ClientSafePipeHandle)));
        }

        [Fact]
        public void RedirectedOutput_RaisesEvents()
        {
            var processIo = new AsyncWindowsProcessIO(StreamingIO);

            ManualResetEvent evt = new ManualResetEvent(false);

            List<string> outputReceived = new List<string>();
            processIo.OutputDataReceived += (o, e) =>
            {
                lock (this)
                {
                    outputReceived.Add(e.Line);
                    if (outputReceived.Count == 2)
                        evt.Set();
                }
            };
            processIo.Start();

            using (StreamWriter writer = new StreamWriter(OutputPipe))
            {
                writer.WriteLine("foo");
                writer.WriteLine("bar");
                writer.Flush();

                Assert.True(evt.WaitOne(5000));

                Assert.Equal(2, outputReceived.Count);
                Assert.Equal("foo", outputReceived[0]);
                Assert.Equal("bar", outputReceived[1]);
            }
        }

        [Fact]
        public void RedirectedError_RaisesEvent()
        {
            var processIo = new AsyncWindowsProcessIO(StreamingIO);

            ManualResetEvent evt = new ManualResetEvent(false);

            List<string> errorReceived = new List<string>();
            processIo.ErrorDataReceived += (o, e) =>
            {
                lock (this)
                {
                    errorReceived.Add(e.Line);
                    if (errorReceived.Count == 2)
                        evt.Set();
                }
            };
            processIo.Start();

            using (StreamWriter writer = new StreamWriter(ErrorPipe))
            {
                writer.WriteLine("foo");
                writer.WriteLine("bar");
                writer.Flush();

                Assert.True(evt.WaitOne(5000));

                Assert.Equal(2, errorReceived.Count);
                Assert.Equal("foo", errorReceived[0]);
                Assert.Equal("bar", errorReceived[1]);
            }
        }

        [Fact]
        public void CanWriteToInput()
        {
            var processIo = new AsyncWindowsProcessIO(StreamingIO);
            processIo.Input.WriteLine("INPUT");
            processIo.Input.Flush();

            using (StreamReader reader = new StreamReader(InputPipe))
            {
                Assert.Equal("INPUT", reader.ReadLine());
            }
        }

        [Fact]
        public void ReturnsStreamingIOHandles()
        {
            var inputHandle = new SafePipeHandle(IntPtr.Zero, false);
            var outputHandle = new SafePipeHandle(IntPtr.Zero, false);
            var errorHandle = new SafePipeHandle(IntPtr.Zero, false);

            StreamingIO.StdInputHandle.Returns(inputHandle);
            StreamingIO.StdOutputHandle.Returns(outputHandle);
            StreamingIO.StdErrorHandle.Returns(errorHandle);

            var processIo = new AsyncWindowsProcessIO(StreamingIO);

            Assert.Equal(inputHandle, processIo.StdInputHandle);
            Assert.Equal(outputHandle, processIo.StdOutputHandle);
            Assert.Equal(errorHandle, processIo.StdErrorHandle);
        }

        [Fact]
        public void WhenDisposed_DisposesStreamingIO()
        {
            var processIo = new AsyncWindowsProcessIO(StreamingIO);
            processIo.Dispose();

            StreamingIO.Received(1).Dispose();
        }

        [Fact]
        public void WaitForAllOutput_ReturnsAfterChildStreamClosed()
        {
            var processIo = new AsyncWindowsProcessIO(StreamingIO);

            List<string> outputReceived = new List<string>();
            processIo.OutputDataReceived += (o, e) =>
            {
                lock (this)
                {
                    outputReceived.Add(e.Line);
                }
            };
            processIo.Start();

            using (StreamWriter writer = new StreamWriter(OutputPipe))
            {
                writer.AutoFlush = true;
                writer.WriteLine("LINE_1");
                writer.WriteLine("LINE_2");
                writer.WriteLine("LINE_3");
                writer.WriteLine("LINE_4");
            }

            Assert.True(processIo.WaitForAllOutput(5000));
            Assert.Equal(4, outputReceived.Count);
        }

        [Fact]
        public void WaitForAllError_ReturnsAfterChildStreamClosed()
        {
            var processIo = new AsyncWindowsProcessIO(StreamingIO);

            List<string> errorReceived = new List<string>();
            processIo.ErrorDataReceived += (o, e) =>
            {
                lock (this)
                {
                    errorReceived.Add(e.Line);
                }
            };
            processIo.Start();

            using (StreamWriter writer = new StreamWriter(ErrorPipe))
            {
                writer.AutoFlush = true;
                writer.WriteLine("LINE_1");
                writer.WriteLine("LINE_2");
                writer.WriteLine("LINE_3");
                writer.WriteLine("LINE_4");
            }

            Assert.True(processIo.WaitForAllError(5000));
            Assert.Equal(4, errorReceived.Count);
        }

        [Fact]
        public void WhileChildstreamOpen_WaitForAllOutput_DoesNotReturn()
        {
            var processIo = new AsyncWindowsProcessIO(StreamingIO);
            processIo.Start();

            using (StreamWriter writer = new StreamWriter(OutputPipe))
            {
                writer.AutoFlush = true;
                writer.WriteLine("LINE_1");
                writer.WriteLine("LINE_2");
                writer.WriteLine("LINE_3");
                writer.WriteLine("LINE_4");

                Assert.False(processIo.WaitForAllOutput(300));
            }
        }

        [Fact]
        public void WhenSubscriberThrowsException_ItDoesntAffectOthers()
        {
            var processIo = new AsyncWindowsProcessIO(StreamingIO);

            List<string> outputReceived = new List<string>();
            processIo.OutputDataReceived += (o, e) => { throw new Exception(); };
            processIo.OutputDataReceived += (o, e) =>
            {
                lock (this)
                {
                    outputReceived.Add(e.Line);
                }
            };
            processIo.OutputDataReceived += (o, e) => { throw new Exception(); };
            processIo.Start();

            using (StreamWriter writer = new StreamWriter(OutputPipe))
            {
                writer.AutoFlush = true;
                writer.WriteLine("LINE_1");
                writer.WriteLine("LINE_2");
            }

            Assert.True(processIo.WaitForAllOutput(5000));
            Assert.Equal(2, outputReceived.Count);
        }

        [Fact]
        public void WhenAlreadyStarted_StartDoesNothing()
        {
            var processIo = new AsyncWindowsProcessIO(StreamingIO);
            processIo.Start();
            processIo.Start();
        }

    }
}
