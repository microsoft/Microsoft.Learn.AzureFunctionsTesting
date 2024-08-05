using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Learn.AzureFunctionsTesting.Core
{
    public interface IFunctionTestConfigurationBuilder
    {
        void RegisterPlugin(IFunctionTestPlugin plugin, string name);

        void ConfigureEnvironmentVariables(Action<Dictionary<string, string>> configure);

        void BeforeProcessStart(Func<Dictionary<string, string>, Task> action);

        void AfterProcessStart(Func<int, Task> action);
    }
}
