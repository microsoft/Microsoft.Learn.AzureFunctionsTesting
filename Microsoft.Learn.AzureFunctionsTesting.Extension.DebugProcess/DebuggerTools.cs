using EnvDTE;
using Microsoft.Learn.AzureFunctionsTesting.Extension.DebugProcess.Core;
using Microsoft.VisualStudio.OLE.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Learn.AzureFunctionsTesting.Extension.DebugProcess
{
    internal static class DebuggerTools
    {
        static List<int>? existingProcessIds = new();

        public static async Task AttachToProcessAsync(int processId)
        {
            var waiter = WaitForSignalAsync(DebuggerConstants.SignalName, TimeSpan.FromSeconds(45));

            MessageFilter.Register();
            var process = GetProcess(processId);
            if (process != null)
            {
                process.Attach();
                System.Diagnostics.Debug.WriteLine($"Automatically attached debugger to func.exe process {processId}");

                // isolated functions spin up yet another child process, so wait for the signal that it has started
                // and then try attaching to that process
                try
                {
                    await waiter;
                    var isoloatedProcess = FindIsolatedProcess();
                    if (isoloatedProcess != null)
                    {
                        isoloatedProcess.Attach();
                        System.Diagnostics.Debug.WriteLine($"Automatically attached debugger to isolated worker process");
                    }
                }
                catch { }
            }
            MessageFilter.Revoke();
        }

        private static Task<bool> WaitForSignalAsync(string signalName, TimeSpan timeout)
        {
            if (!OperatingSystem.IsWindows())
            {
                throw new PlatformNotSupportedException();
            }

            using var ewh = new EventWaitHandle(false, EventResetMode.ManualReset, signalName);

            // optimize for special cases
            var alreadySignalled = ewh.WaitOne(0);
            if (alreadySignalled)
                return Task.FromResult(true);
            if (timeout == TimeSpan.Zero)
                return Task.FromResult(false);

            var tcs = new TaskCompletionSource<bool>();
            var threadPoolRegistration = ThreadPool.RegisterWaitForSingleObject(ewh, (state, timedOut) => ((TaskCompletionSource<bool>?)state)?.TrySetResult(!timedOut), tcs, timeout, true);
            return tcs.Task;
        }

        private static Process? GetProcess(int processID)
        {
            var processes = GetProcesses();
            return Try(() =>
            {
                existingProcessIds = processes?.Select(x => x.ProcessID).ToList();
                return processes?.SingleOrDefault(x => x.ProcessID == processID);
            });
        }

        private static Process? FindIsolatedProcess()
        {
            var processes = GetProcesses();
            return Try(() =>
            {
                var newProcesses = processes?.ExceptBy(existingProcessIds!, x => x.ProcessID);
                return newProcesses?.SingleOrDefault(x => x.Name.EndsWith("dotnet.exe"));
            });
        }

        private static IEnumerable<Process>? GetProcesses()
        {
            var thisVs = GetThisVsInstance();
            return Try(() =>
            {
                return thisVs?.Debugger.LocalProcesses.OfType<Process>();
            });
        }

        private static DTE? GetThisVsInstance()
        {
            var vsInstances = VS.GetInstances();
            return Try(() =>
            {
                var pid = System.Diagnostics.Process.GetCurrentProcess().Id;
                foreach (var vsInstance in vsInstances)
                {
                    if (vsInstance.Debugger.DebuggedProcesses.Count > 0 && vsInstance.Debugger.CurrentMode == dbgDebugMode.dbgRunMode)
                    {
                        foreach (var debuggedProcess in vsInstance.Debugger.DebuggedProcesses)
                        {
                            var debuggedProcessId = ((Process)debuggedProcess).ProcessID;
                            if (debuggedProcessId == pid)
                            {
                                return vsInstance;
                            }
                        }
                    }
                }
                return null;
            });
        }

        // The message filter will often report 'instance in use, try again' so we just keep retrying until it works.
        // This is only generally an issue if you are debugging the debugger attachment - in normal usage, it always works on the first pass.
        private static T Try<T>(Func<T> func)
        {
            while (true)
            {
                try
                {
                    return func();
                }
                catch
                {

                }
            }
        }
    }


    [ComImport, Guid("00000016-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IOleMessageFilter
    {
        [PreserveSig]
        int HandleInComingCall(int dwCallType, IntPtr hTaskCaller, int dwTickCount, IntPtr lpInterfaceInfo);

        [PreserveSig]
        int RetryRejectedCall(IntPtr hTaskCallee, int dwTickCount, int dwRejectType);

        [PreserveSig]
        int MessagePending(IntPtr hTaskCallee, int dwTickCount, int dwPendingType);
    }

    internal class MessageFilter : IOleMessageFilter
    {
        private const int Handled = 0, RetryAllowed = 2, Retry = 99, Cancel = -1, WaitAndDispatch = 2;

        int IOleMessageFilter.HandleInComingCall(int dwCallType, IntPtr hTaskCaller, int dwTickCount, IntPtr lpInterfaceInfo)
        {
            return Handled;
        }

        int IOleMessageFilter.RetryRejectedCall(IntPtr hTaskCallee, int dwTickCount, int dwRejectType)
        {
            return dwRejectType == RetryAllowed ? Retry : Cancel;
        }

        int IOleMessageFilter.MessagePending(IntPtr hTaskCallee, int dwTickCount, int dwPendingType)
        {
            return WaitAndDispatch;
        }

        public static void Register()
        {
            CoRegisterMessageFilter(new MessageFilter());
        }

        public static void Revoke()
        {
            CoRegisterMessageFilter(null);
        }

        private static void CoRegisterMessageFilter(IOleMessageFilter? newFilter)
        {
            IOleMessageFilter oldFilter;
            CoRegisterMessageFilter(newFilter, out oldFilter);
        }

        [DllImport("Ole32.dll")]
        private static extern int CoRegisterMessageFilter(IOleMessageFilter? newFilter, out IOleMessageFilter oldFilter);
    }

    internal class VS
    {
        public static IEnumerable<DTE> GetInstances()
        {
            IRunningObjectTable rot;
            IEnumMoniker enumMoniker;
            int retVal = GetRunningObjectTable(0, out rot);

            if (retVal == 0)
            {
                rot.EnumRunning(out enumMoniker);

                uint fetched = 0;
                IMoniker[] moniker = new IMoniker[1];
                while (enumMoniker.Next(1, moniker, out fetched) == 0)
                {
                    IBindCtx bindCtx;
                    CreateBindCtx(0, out bindCtx);
                    string displayName;
                    moniker[0].GetDisplayName(bindCtx, null, out displayName);
                    Console.WriteLine("Display Name: {0}", displayName);
                    bool isVisualStudio = displayName.StartsWith("!VisualStudio");
                    if (isVisualStudio)
                    {
                        rot.GetObject(moniker[0], out var obj);
                        var dte = obj as DTE;
                        yield return dte!;
                    }
                }
            }
        }

        [DllImport("ole32.dll")]
        private static extern void CreateBindCtx(int reserved, out IBindCtx ppbc);

        [DllImport("ole32.dll")]
        private static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable prot);
    }
}
