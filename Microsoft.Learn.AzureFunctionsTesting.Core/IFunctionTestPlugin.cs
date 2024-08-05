using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Learn.AzureFunctionsTesting.Core
{
    public interface IFunctionTestPlugin
    {
        Task InitializeAsync(Dictionary<string, string> environmentVars);

        Task DisposeAsync();
    }
}