using System;
using System.Diagnostics;
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

        private bool _started = false;

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
            if (!_started)
            {
                _streamIO.Start();
                StartListeningForOutputLine();
                StartListeningForErrorLine();
                _started = true;
            }
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
                OnLineReceived(OutputDataReceived, line);
                // By starting to listen for the next line only after publishing the current line
                // we guarantee that subscribers are receiving the lines in order.
                // It does, however, introduce additional latency in the publishing of lines.
                StartListeningForOutputLine();
            }
            else
            {
                _allOutputReceived.Set();
            }
        }

        private void StartListeningForErrorLine()
        {
            _streamIO.Error.ReadLineAsync().ContinueWith(t => OnErrorLineReceived(t.Result));
        }

        private void OnErrorLineReceived(string line)
        {
            if (line != null)
            {
                OnLineReceived(ErrorDataReceived, line);
                // By starting to listen for the next line only after publishing the current line
                // we guarantee that subscribers are receiving the lines in order.
                // It does, however, introduce additional latency in the publishing of lines.
                StartListeningForErrorLine();
            }
            else
            {
                _allErrorReceived.Set();
            }
        }

        private void OnLineReceived(EventHandler<LineReceivedEventArgs> handler, string line)
        {
            if (handler != null)
            {
                var args = new LineReceivedEventArgs(line);
                var delegateList = handler.GetInvocationList();

                foreach (Delegate del in delegateList)
                {
                    try
                    {
                        del.DynamicInvoke(this, args);
                    }
                    catch
                    {
                        // There is no good action to take here.
                        // If we throw, it just bubbles up to the task reading lines.
                        // The only way to inform the user of the instance is to save it and rethrow
                        // whenever they call some method on the instance.
                    }
                }
            }
        }

    }
}
