using System;
using System.IO;
using System.Threading;

namespace WindowsProcess.Utilities
{
    interface IAsyncLineReader : IDisposable
    {
        event EventHandler<LineReceivedEventArgs> OutputLineReceived;
        void Start();
        bool WaitForAllOutput(int milliseconds);
    }

    internal class AsyncLineReader : IAsyncLineReader
    {
        private readonly ManualResetEvent _allOutputReceived = new ManualResetEvent(false);
        private readonly StreamReader _reader;
        private bool _started;

        public event EventHandler<LineReceivedEventArgs> OutputLineReceived;

        public AsyncLineReader(StreamReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException("reader");

            _reader = reader;
        }

        public void Start()
        {
            if (!_started)
            {
                StartListeningForOutputLine();
                _started = true;
            }
        }

        public bool WaitForAllOutput(int milliseconds)
        {
            return _allOutputReceived.WaitOne(milliseconds);
        }

        private void StartListeningForOutputLine()
        {
            _reader.ReadLineAsync().ContinueWith(t => OnOutputLineReceived(t.Result));
        }

        public void Dispose()
        {
            _reader.Dispose();
            // Let all waiting threads know there won't be any more output coming.
            _allOutputReceived.Set();
        }

        private void OnOutputLineReceived(string line)
        {
            if (line != null)
            {
                OnLineReceived(line);
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

        private void OnLineReceived(string line)
        {
            var handler = OutputLineReceived;

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
