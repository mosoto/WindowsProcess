﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using WindowsProcess.Utilities;
using Xunit;

namespace WindowsProcess.Tests
{
    public class WindowsProcessTests
    {
        private readonly string WindowsProcessTestHelperPath = typeof(WindowsProcessTestHelper.Program).Assembly.Location;
        private readonly string CmdExeFullPath = Environment.SystemDirectory + @"\cmd.exe";
        WindowsProcessStartInfo StartInfo { get; set; }

        public WindowsProcessTests()
        {
            StartInfo = new WindowsProcessStartInfo()
            {
                AutoStart = true,
                CreateNoWindow = true
            };
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

            public class WithRedirection : Create
            {
                [Fact]
                public void OnlyOutput()
                {
                    var io = new StreamingWindowsProcessIO(false, true, false);
                    StartInfo.IO = io;

                    var process = StartProcessWithOutput();
                    process.WaitForExit();

                    string output = io.Output.ReadToEnd();

                    Assert.Contains("OUTPUT", output);
                }

                [Fact]
                public void OutputAndError()
                {
                    var io = new StreamingWindowsProcessIO(false, true, true);
                    StartInfo.IO = io;

                    var process = StartProcessWithOutput();
                    process.WaitForExit();

                    string output = io.Output.ReadToEnd();
                    string error = io.Error.ReadToEnd();

                    Assert.Contains("OUTPUT", output);
                    Assert.Contains("ERROR", error);
                }

                [Fact]
                public void AllPipes()
                {
                    var io = new StreamingWindowsProcessIO(true, true, true);

                    StartInfo.FileName = WindowsProcessTestHelperPath;
                    StartInfo.IO = io;
                    StartInfo.Arguments = "OUTPUT ERROR";
                    StartInfo.AutoStart = true;

                    var process = WindowsProcess.Create(StartInfo);
                    io.Input.WriteLine("INPUT_LINE");
                    io.Input.Close();
                    process.WaitForExit();

                    string output = io.Output.ReadToEnd();
                    string error = io.Error.ReadToEnd();

                    Assert.Contains("INPUT_LINE", output);
                    Assert.Contains("INPUT_LINE", error);
                }
            }

            public class WhenWorkingDirectorySet : Create
            {
                [Fact]
                public void StartsProcessInDirectory()
                {
                    var io = new StreamingWindowsProcessIO(false, true, false);

                    StartInfo.FileName = CmdExeFullPath;
                    StartInfo.Arguments = "/C cd";
                    StartInfo.AutoStart = true;
                    StartInfo.IO = io;
                    StartInfo.WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

                    var process = WindowsProcess.Create(StartInfo);
                    process.WaitForExit();

                    string actual = io.Output.ReadLine();

                    Assert.Equal(StartInfo.WorkingDirectory, actual);
                }

                [Fact]
                public void NullWorkingDirectorySetsCurrentDirectory()
                {
                    var io = new StreamingWindowsProcessIO(false, true, false);

                    StartInfo.FileName = CmdExeFullPath;
                    StartInfo.Arguments = "/C cd";
                    StartInfo.AutoStart = true;
                    StartInfo.IO = io;
                    StartInfo.WorkingDirectory = null;

                    var process = WindowsProcess.Create(StartInfo);
                    process.WaitForExit();

                    string actual = io.Output.ReadLine();

                    Assert.Equal(Environment.CurrentDirectory, actual);
                }
            }

            public class WhenAutoStartIsFalse : Create
            {
                [Fact]
                public void StartsProcessSuspended()
                {
                    StartInfo.AutoStart = false;

                    var process = StartImmediatelyExitingProcess();
                    process.WaitForExit(200);
                    Assert.False(process.HasExited);
                    process.Start();
                    process.WaitForExit(200);
                    Assert.True(process.HasExited);
                }
            }

            public class WhenEnvironmentSpecified : Create
            {
                [Fact]
                public void CreatesProcessWithEnvironment()
                {
                    var io = new StreamingWindowsProcessIO(false, true, false);

                    StartInfo.FileName = CmdExeFullPath;
                    StartInfo.Arguments = "/C set";
                    StartInfo.AutoStart = true;
                    StartInfo.IO = io;

                    StartInfo.Environment = Environment.GetEnvironmentVariables().ToStringDictionary();
                    StartInfo.Environment["TEST_ENVIRONMENT_VALUE1"] = "FOO";
                    StartInfo.Environment["TEST_ENVIRONMENT_VALUE2"] = "BAR";

                    var process = WindowsProcess.Create(StartInfo);
                    string[] outputLines = io.Output.ReadToEnd().Split(new [] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                    Assert.Contains("TEST_ENVIRONMENT_VALUE1=FOO", outputLines);
                    Assert.Contains("TEST_ENVIRONMENT_VALUE2=BAR", outputLines);
                }
            }

            public class WhenCredentialsProvided : Create
            {
                [FactAdminRequired]
                public void CreatesProcessAsOtherUser()
                {
                    var io = new StreamingWindowsProcessIO(false, true, false);

                    using (var tempUser = UserFactory.CreateTempUser())
                    {
                        StartInfo.FileName = "whoami.exe";
                        StartInfo.AutoStart = true;
                        StartInfo.IO = io;
                        StartInfo.Credential = tempUser.Credential;

                        var process = WindowsProcess.Create(StartInfo);
                        process.WaitForExit();
                        var output = io.Output.ReadToEnd();

                        var username = output.Trim().Split(new[] {'\\'})[1];

                        Assert.Equal(tempUser.Credential.UserName, username);
                    }
                }

                [FactAdminRequired]
                public void CreatesProcessWithSpecifiedEnvironment()
                {
                    var io = new StreamingWindowsProcessIO(false, true, false);

                    using (var tempUser = UserFactory.CreateTempUser())
                    {
                        StartInfo.FileName = CmdExeFullPath;
                        StartInfo.Arguments = "/C set";
                        StartInfo.AutoStart = true;
                        StartInfo.IO = io;
                        StartInfo.LoadUserProfile = true;
                        StartInfo.WorkingDirectory = Environment.SystemDirectory;
                        StartInfo.Credential = tempUser.Credential;
                        StartInfo.Environment = EnvironmentBlock.CreateSystemDefault().ToDictionary();
                        StartInfo.Environment["FOO"] = "BAR";

                        var process = WindowsProcess.Create(StartInfo);
                        var output = io.Output.ReadToEnd();

                        Assert.Contains("FOO=BAR", output);
                    }
                }

                public class WithLoadUserProfile : WhenCredentialsProvided
                {
                    [FactAdminRequired]
                    public void ProcessHasUsersProfile()
                    {
                        var io = new StreamingWindowsProcessIO(false, true, false);

                        using (var tempUser = UserFactory.CreateTempUser())
                        {
                            StartInfo.FileName = CmdExeFullPath;
                            StartInfo.Arguments = "/C set";
                            StartInfo.AutoStart = true;
                            StartInfo.IO = io;
                            StartInfo.LoadUserProfile = true;
                            StartInfo.WorkingDirectory = Environment.SystemDirectory;
                            StartInfo.Credential = tempUser.Credential;

                            var process = WindowsProcess.Create(StartInfo);
                            var output = io.Output.ReadToEnd();

                            Assert.Contains("USERNAME" + "=" + tempUser.Credential.UserName, output);
                        }
                    }
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
        }

        public class Start : WindowsProcessTests
        {
            [Fact]
            public void ResumesASuspendedProcess()
            {
                StartInfo.AutoStart = false;
                
                var process = StartImmediatelyExitingProcess();

                Assert.False(process.HasExited);
                process.Start();
                process.WaitForExit(300);
                Assert.True(process.HasExited);
            }

            [Fact]
            public void WhenProcessRunning_DoesNothing()
            {
                var process = StartShortLivedProcess();

                Assert.False(process.HasExited);
                process.Start();
                process.Start();
                process.Start();
            }
        }

        protected IWindowsProcess StartImmediatelyExitingProcess()
        {
            StartInfo.FileName = CmdExeFullPath;
            StartInfo.Arguments = "/C";

            return WindowsProcess.Create(StartInfo);
        }

        protected IWindowsProcess StartShortLivedProcess()
        {
            StartInfo.FileName = CmdExeFullPath;
            StartInfo.Arguments = "/C ping 127.0.0.1 -n 4";

            return WindowsProcess.Create(StartInfo);
        }

        protected IWindowsProcess StartProcessWithOutput()
        {
            StartInfo.FileName = CmdExeFullPath;
            StartInfo.Arguments = "/C echo OUTPUT && echo ERROR >&2 ";

            return WindowsProcess.Create(StartInfo);
        }
    }
}
