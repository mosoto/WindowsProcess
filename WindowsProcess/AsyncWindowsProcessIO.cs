using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable InconsistentNaming

namespace WindowsProcess
{
    public class AsyncWindowsProcessIO : IWindowsProcessIO
    {
        private readonly IStreamingWindowsProcessIO _streamIO;
        private readonly ManualResetEvent _allOutputReceived = new ManualResetEvent(false);
        private readonly ManualResetEvent _allErrorReceived = new ManualResetEvent(false);

        public AsyncWindowsProcessIO(IStreamingWindowsProcessIO streamingIO)
        {
            _streamIO = streamingIO;
        }

        public AsyncWindowsProcessIO(bool redirectInput, bool redirectOutput, bool redirectError)
            : this(new StreamingWindowsProcessIO(redirectInput, redirectOutput, redirectError))
        {
        }

        public bool WaitForAllOutput(int milliseconds)
        {
            return _allOutputReceived.WaitOne(milliseconds);
        }

        public bool WaitForAllError(int milliseconds)
        {
            return _allErrorReceived.WaitOne(milliseconds);
        }

        public event EventHandler<LineReceivedEventArgs> OutputDataReceived;
        public event EventHandler<LineReceivedEventArgs> ErrorDataReceived;
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
            StartListeningForOutputLine();
            StartListeningForErrorLine();
        }

        public void Dispose()
        {
            _streamIO.Dispose();
        }

        private void StartListeningForOutputLine()
        {
            _streamIO.Output.ReadLineAsync().ContinueWith(t => OnOutputLineReceived(t.Result));
        }

        private void OnOutputLineReceived(string line)
        {
            if (line != null)
            {
                StartListeningForOutputLine();
                OnLineReceived(OutputDataReceived, line);
            }
        }

        private void StartListeningForErrorLine()
        {
            _streamIO.Error.ReadLineAsync().ContinueWith(t => OnErrorLineReceived(t.Result));
        }

        private void OnErrorLineReceived(string line)
        {
            StartListeningForErrorLine();
            OnLineReceived(ErrorDataReceived, line);
        }

        private void OnLineReceived(EventHandler<LineReceivedEventArgs> handler, string line)
        {
            if (handler != null)
            {
                var args = new LineReceivedEventArgs(line);
                handler(this, args);
            }
        }

    }
}
