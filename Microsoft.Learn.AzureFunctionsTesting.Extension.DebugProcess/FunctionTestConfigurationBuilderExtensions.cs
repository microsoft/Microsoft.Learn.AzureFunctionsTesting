using Microsoft.Learn.AzureFunctionsTesting.Core;
using Microsoft.Learn.AzureFunctionsTesting.Extension.DebugProcess.Core;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.Learn.AzureFunctionsTesting.Extension.DebugProcess
{
    public static class FunctionTestConfigurationBuilderExtensions
    {
        public static void DebugIntoFunctions(this IFunctionTestConfigurationBuilder builder)
        {
            builder.BeforeProcessStart(envVars =>
            {
                if (Debugger.IsAttached)
                {
                    envVars[DebuggerConstants.SignalName] = true.ToString();
                }
                return Task.CompletedTask;
            });
            builder.AfterProcessStart(async processId =>
            {
                if (Debugger.IsAttached)
                {
                    await DebuggerTools.AttachToProcessAsync(processId);
                }
            });
        }
    }
}