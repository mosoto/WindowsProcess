using System;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;

namespace WindowsProcess
{
    // ReSharper disable InconsistentNaming
    public class StreamingWindowsProcessIO : IWindowsProcessIO, IDisposable
    {
        private readonly StreamWriter _inputWriter;
        private readonly StreamReader _outputReader;
        private readonly StreamReader _errorReader;

        public StreamingWindowsProcessIO(bool redirectInput, bool redirectOutput, bool redirectError)
        {
            const HandleInheritability inheritability = HandleInheritability.Inheritable;

            if (redirectInput)
            {
                var inputStream = new AnonymousPipeServerStream(PipeDirection.Out, inheritability);
                StdInputHandle = inputStream.ClientSafePipeHandle;
                _inputWriter = new StreamWriter(inputStream);
            }

            if (redirectOutput)
            {
                var outputStream = new AnonymousPipeServerStream(PipeDirection.In, inheritability);
                StdOutputHandle = outputStream.ClientSafePipeHandle;
                _outputReader = new StreamReader(outputStream);
            }

            if (redirectError)
            {
                var errorStream = new AnonymousPipeServerStream(PipeDirection.In, inheritability);
                StdErrorHandle = errorStream.ClientSafePipeHandle;
                _errorReader = new StreamReader(errorStream);
            }
        }

        public StreamWriter InputStream
        {
            get
            {
                ThrowIfNull(_inputWriter);
                return _inputWriter;
            }
        }

        public StreamReader OutputStream
        {
            get
            {
                ThrowIfNull(_outputReader);
                return _outputReader;
            }
        }

        public StreamReader ErrorStream
        {
            get
            {
                ThrowIfNull(_errorReader);
                return _errorReader;
            }
        }

        public SafeHandle StdInputHandle { get; private set; }

        public SafeHandle StdOutputHandle { get; private set; }

        public SafeHandle StdErrorHandle { get; private set; }

        public void Start()
        {
            // Dispose our copies of the child process handles
            // See https://msdn.microsoft.com/en-us/library/system.io.pipes.anonymouspipeserverstream.disposelocalcopyofclienthandle(v=vs.110).aspx
            DisposeIfNotNull(StdInputHandle);
            DisposeIfNotNull(StdOutputHandle);
            DisposeIfNotNull(StdErrorHandle);
        }

        private static void DisposeIfNotNull(IDisposable disposable)
        {
            if (disposable != null)
            {
                disposable.Dispose();
            }
        }

        // ReSharper disable once UnusedParameter.Local
        private static void ThrowIfNull(object obj)
        {
            if (obj == null)
                throw new InvalidOperationException("This stream was not redirected!");
        }

        public void Dispose()
        {
            DisposeIfNotNull(_inputWriter);
            DisposeIfNotNull(_outputReader);
            DisposeIfNotNull(_errorReader);
        }
    }
}
