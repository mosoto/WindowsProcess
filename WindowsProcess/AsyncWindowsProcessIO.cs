using System;
using System.IO;
using System.Runtime.InteropServices;
using WindowsProcess.Utilities;

// ReSharper disable InconsistentNaming

namespace WindowsProcess
{
    public class AsyncWindowsProcessIO : IWindowsProcessIO
    {
        private readonly IStreamingWindowsProcessIO _streamIO;
        private readonly IAsyncLineReader _outputReader;
        private readonly IAsyncLineReader _errorReader;


        public AsyncWindowsProcessIO(IStreamingWindowsProcessIO streamingIO)
        {
            _streamIO = streamingIO;

            _outputReader = _streamIO.Output == null 
                ? (IAsyncLineReader) new NullAsyncLineReader() 
                : new AsyncLineReader(_streamIO.Output);

            _errorReader = _streamIO.Error == null
                ? (IAsyncLineReader)new NullAsyncLineReader()
                : new AsyncLineReader(_streamIO.Error);
        }

        public AsyncWindowsProcessIO(bool redirectInput, bool redirectOutput, bool redirectError)
            : this(new StreamingWindowsProcessIO(redirectInput, redirectOutput, redirectError))
        {
        }

        public bool WaitForAllOutput(int milliseconds)
        {
            return _outputReader.WaitForAllOutput(milliseconds);
        }

        public bool WaitForAllError(int milliseconds)
        {
            return _errorReader.WaitForAllOutput(milliseconds);
        }

        public event EventHandler<LineReceivedEventArgs> OutputDataReceived
        {
            add { _outputReader.OutputLineReceived += value; }
            remove { _outputReader.OutputLineReceived -= value; }
        }

        public event EventHandler<LineReceivedEventArgs> ErrorDataReceived
        {
            add { _errorReader.OutputLineReceived += value; }
            remove { _errorReader.OutputLineReceived -= value; }
        }

        public StreamWriter Input
        {
            get { return _streamIO.Input; }
        }

        public SafeHandle StdInputHandle
        {
            get { return _streamIO.StdInputHandle; }
        }

        public SafeHandle StdOutputHandle
        {
            get { return _streamIO.StdOutputHandle; }
        }

        public SafeHandle StdErrorHandle
        {
            get { return _streamIO.StdErrorHandle; }
        }

        public void Start()
        {
            _streamIO.Start();
            _outputReader.Start();
            _errorReader.Start();
        }

        public void Dispose()
        {
            _streamIO.Dispose();
            _outputReader.Dispose();
            _errorReader.Dispose();
        }

        private class NullAsyncLineReader : IAsyncLineReader
        {
            public event EventHandler<LineReceivedEventArgs> OutputLineReceived;

            public void Start()
            {
            }

            public bool WaitForAllOutput(int milliseconds)
            {
                return true;
            }

            public void Dispose()
            {
            }
        }

    }
}
