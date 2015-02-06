using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace WindowsProcess.Tests
{
    public class WindowsProcessTests
    {
        private readonly string CmdExeFullPath = Environment.SystemDirectory + @"\cmd.exe";
        WindowsProcessStartInfo StartInfo { get; set; }

        public WindowsProcessTests()
        {
            StartInfo = new WindowsProcessStartInfo();
        }

        public class Create : WindowsProcessTests
        {
            [Fact]
            public void WithNullFileName_Throws()
            {
                Action action = () => WindowsProcess.Create(StartInfo);
                Assert.Throws<ArgumentException>(action);
            }

            [Fact]
            public void WithFileNameAndArguments_StartsProcess()
            {
                var process = StartImmediatelyExitingProcess();

                Assert.NotNull(process);
            }

            [Fact]
            public void WithFullPath()
            {
                StartInfo.FileName = CmdExeFullPath;
                StartInfo.Arguments = "/C";

                var process = WindowsProcess.Create(StartInfo);

                Assert.NotNull(process);
            }

            public class WithRedirectedPipes : Create
            {
                [Fact]
                public void OnlyOutput()
                {
                    StartInfo.FileName = CmdExeFullPath;
                    StartInfo.Arguments = "/C echo foo";
                    StartInfo.RedirectStandardOutput = true;
                    StartInfo.AutoStart = true;

                    var process = WindowsProcess.Create(StartInfo);
                    process.WaitForExit();

                    string output = process.IO.Output.ReadToEnd();

                    Assert.Contains("foo", output);
                }

                [Fact]
                public void OnlyError()
                {
                    StartInfo.FileName = CmdExeFullPath;
                    StartInfo.Arguments = "/C echo foo >&2";
                    StartInfo.RedirectStandardError = true;
                    StartInfo.AutoStart = true;

                    var process = WindowsProcess.Create(StartInfo);
                    process.WaitForExit();

                    string error = process.IO.Error.ReadLine();

                    Assert.Contains("foo", error);
                }

                [Fact]
                public void AllPipes()
                {
                    StartInfo.FileName = CmdExeFullPath;
                    StartInfo.Arguments = "/C echo foo && echo bar >&2";
                    StartInfo.RedirectStandardInput = true;
                    StartInfo.RedirectStandardOutput = true;
                    StartInfo.RedirectStandardError = true;
                    StartInfo.AutoStart = true;

                    var process = WindowsProcess.Create(StartInfo);
                    process.WaitForExit();

                    string output = process.IO.Output.ReadToEnd();
                    string error = process.IO.Error.ReadToEnd();

                    Assert.Contains("foo", output);
                    Assert.Contains("bar", error);
                }

                [Fact]
                public void WhenReadUnredirectedPipe_Throws()
                {
                    StartInfo.FileName = CmdExeFullPath;
                    StartInfo.Arguments = "/C echo foo && echo bar >&2";
                    StartInfo.AutoStart = true;

                    var process = WindowsProcess.Create(StartInfo);
                    process.WaitForExit();

                    Assert.Throws<InvalidOperationException>(() => process.IO.Input.WriteLine("foo"));
                    Assert.Throws<InvalidOperationException>(() => process.IO.Output.ReadLine());
                    Assert.Throws<InvalidOperationException>(() => process.IO.Error.ReadLine());
                }
            }

            public class WhenWorkingDirectorySet : Create
            {
                [Fact]
                public void StartsProcessInDirectory()
                {
                    StartInfo.FileName = CmdExeFullPath;
                    StartInfo.Arguments = "/C cd";
                    StartInfo.AutoStart = true;
                    StartInfo.RedirectStandardOutput = true;
                    StartInfo.WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

                    var process = WindowsProcess.Create(StartInfo);
                    process.WaitForExit();

                    string actual = process.IO.Output.ReadLine();

                    Assert.Equal(StartInfo.WorkingDirectory, actual);
                }

                [Fact]
                public void NullWorkingDirectorySetsCurrentDirectory()
                {
                    StartInfo.FileName = CmdExeFullPath;
                    StartInfo.Arguments = "/C cd";
                    StartInfo.AutoStart = true;
                    StartInfo.RedirectStandardOutput = true;
                    StartInfo.WorkingDirectory = null;

                    var process = WindowsProcess.Create(StartInfo);
                    process.WaitForExit();

                    string actual = process.IO.Output.ReadLine();

                    Assert.Equal(Environment.CurrentDirectory, actual);
                }
            }
        }

        public class WaitForExit : WindowsProcessTests
        {
            [Fact]
            public void ReturnsIfProcessExited()
            {
                StartInfo.FileName = CmdExeFullPath;
                StartInfo.Arguments = "/C";

                var process = WindowsProcess.Create(StartInfo);
                process.WaitForExit(Timeout.Infinite);
            }

            [Fact]
            public void CanBeCalledMultipleTimes()
            {
                StartInfo.FileName = CmdExeFullPath;
                StartInfo.Arguments = "/C";

                var process = WindowsProcess.Create(StartInfo);
                Assert.True(process.WaitForExit(Timeout.Infinite));
                Assert.True(process.WaitForExit(Timeout.Infinite));
                Assert.True(process.WaitForExit(Timeout.Infinite));
            }
        }

        public class Id : WindowsProcessTests
        {
            [Fact]
            public void ReturnsTheProcessId()
            {
                StartInfo.FileName = CmdExeFullPath;
                StartInfo.Arguments = "/C";

                var process = WindowsProcess.Create(StartInfo);

                Assert.NotEqual(0, process.Id);
            }

            [Fact]
            public void WhenDisposed_Throws()
            {
                var process = StartShortLivedProcess();

                process.Dispose();
                Func<object> action = () => process.Id ;

                Assert.Throws<InvalidOperationException>(action);
            }

        }

        public class Kill : WindowsProcessTests
        {
            [Fact]
            public void TerminatesProcessAndSetsExitCode()
            {
                var process = StartShortLivedProcess();

                process.Kill(-8);
                process.WaitForExit();

                Assert.True(process.HasExited);
                Assert.Equal(-8, process.ExitCode);
            }

            [Fact]
            public void ThrowsIfProcessExited()
            {
                var process = StartShortLivedProcess();

                process.Kill(-8);
                process.WaitForExit();

                Action action = () => process.Kill(-9);
                Assert.Throws<Win32Exception>(action);
            }

            [Fact]
            public void WhenDisposed_Throws()
            {
                var process = StartShortLivedProcess();

                process.Dispose();
                Action action = () => process.Kill();

                Assert.Throws<InvalidOperationException>(action);
            }
        }

        public class Exited : WindowsProcessTests
        {
            [Fact]
            public void WhenProcessTerminates_IsRaised()
            {
                var process = StartShortLivedProcess();

                ManualResetEvent waitEvent = new ManualResetEvent(false);
                object sender = null;
                WindowsProcessExitedEventArgs args = null;

                process.Exited += (o, e) =>
                {
                    sender = o;
                    args = e; 
                    waitEvent.Set(); 
                };
                
                process.Kill(-2);
                process.WaitForExit();

                if (waitEvent.WaitOne(2000))
                {
                    Assert.Equal(sender, process);
                    Assert.Equal(-2, args.ExitCode);
                }
                else
                {
                    Assert.True(false, "Process exit event was not raised.");
                }
            }
        }

        public class Dispose : WindowsProcessTests
        {
            [Fact]
            public void IsIdempotent()
            {
                var process = StartShortLivedProcess();

                process.Dispose();
                process.Dispose();
            }

            [Fact]
            public void DoesNotKillProcess()
            {
                var process = StartShortLivedProcess();
                var proc = System.Diagnostics.Process.GetProcessById(process.Id);

                process.Dispose();

                Assert.False(proc.HasExited);
                proc.Kill();
            }

            [Fact]
            public void DisposesProcessIO()
            {
                StartInfo.RedirectStandardInput = true;
                StartInfo.RedirectStandardOutput = true;
                StartInfo.RedirectStandardError = true;

                var process = StartImmediatelyExitingProcess();
                process.Dispose();

                Assert.Throws<InvalidOperationException>(() => process.IO.Input.WriteLine("foo"));
                Assert.Throws<InvalidOperationException>(() => process.IO.Output.ReadLine());
                Assert.Throws<InvalidOperationException>(() => process.IO.Error.ReadLine());
            }
        }

        protected IWindowsProcess StartImmediatelyExitingProcess()
        {
            StartInfo.FileName = CmdExeFullPath;
            StartInfo.Arguments = "/C";
            StartInfo.AutoStart = true;

            return WindowsProcess.Create(StartInfo);
        }

        protected IWindowsProcess StartShortLivedProcess()
        {
            StartInfo.FileName = CmdExeFullPath;
            StartInfo.Arguments = "/C ping 127.0.0.1 -n 4";
            StartInfo.AutoStart = true;

            return WindowsProcess.Create(StartInfo);
        }

        [Fact]
        protected IWindowsProcess StartNonExitingProcess()
        {
            StartInfo.FileName = CmdExeFullPath;
            StartInfo.Arguments = "/C ping 127.0.0.1 -t";
            StartInfo.AutoStart = true;

            return WindowsProcess.Create(StartInfo);
        }
    }
}
