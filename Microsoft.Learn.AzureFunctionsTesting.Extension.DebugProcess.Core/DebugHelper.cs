using System;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Threading;

namespace Microsoft.Learn.AzureFunctionsTesting.Extension.DebugProcess.Core
{
    [SupportedOSPlatform("windows")]
    public static class DebugHelper
    {
        public static void WaitForDebuggerToAttach()
        {
            if (Environment.GetEnvironmentVariable(DebuggerConstants.SignalName) == true.ToString())
            {
                SendSignal(DebuggerConstants.SignalName);
                while (!Debugger.IsAttached)
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(100));
                }
            }
        }

        private static void SendSignal(string signalName)
        {
            if (!OperatingSystem.IsWindows())
            {
                throw new PlatformNotSupportedException();
            }

            if (!string.IsNullOrWhiteSpace(signalName))
            {
                if (EventWaitHandle.TryOpenExisting(signalName, out var ewh))
                {
                    ewh.Set();
                }
                else
                {
                    Debug.WriteLine($"Could not send signal - no existing EventWaitHandle found named '{signalName}'");
                }
            }
            else
            {
                Debug.WriteLine("Could not send signal - no signalName specified");
            }
        }
    }
}