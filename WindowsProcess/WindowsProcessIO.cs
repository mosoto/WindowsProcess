using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

// ReSharper disable InconsistentNaming

namespace WindowsProcess
{
    public interface IWindowsProcessIO : IDisposable
    {
        StreamWriter Input { get; }
        StreamReader Output { get; }
        StreamReader Error { get; }
    }


    public class WindowsProcessIO : IWindowsProcessIO
    {
        private StreamWriter _inputStream;
        private StreamReader _outputStream;
        private StreamReader _errorStream;

        public WindowsProcessIO(
            Stream stdInputStream,
            Stream stdOutputStream,
            Stream stdErrorStream)
        {
            if (stdInputStream != null)
            {
                _inputStream = new StreamWriter(stdInputStream);
            }

            if (stdOutputStream != null)
            {
                _outputStream = new StreamReader(stdOutputStream);
            }

            if (stdErrorStream != null)
            {
                _errorStream = new StreamReader(stdErrorStream);
            }
        }

        public StreamWriter Input
        {
            get { return ThrowIfNull(_inputStream); }
        }

        public StreamReader Output
        {
            get { return ThrowIfNull(_outputStream); }
        }

        public StreamReader Error
        {
            get { return ThrowIfNull(_errorStream); }
        }

        private T ThrowIfNull<T>(T stream)
        {
            if (stream == null)
                throw new InvalidOperationException("The stream was not redirected.");

            return stream;
        }
        public void Dispose()
        {
            if (_inputStream != null)
            {
                _inputStream.Close();
            }

            if (_outputStream != null)
            {
                _outputStream.Close();
            }

            if (_errorStream != null)
            {
                _errorStream.Close();
            }
        }
    }
}
