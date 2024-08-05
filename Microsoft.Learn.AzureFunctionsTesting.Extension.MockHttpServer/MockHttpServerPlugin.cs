using Microsoft.Learn.AzureFunctionsTesting.Core;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Learn.AzureFunctionsTesting.Extension.MockHttpServer
{
    public class MockHttpServerPlugin : IFunctionTestPlugin
    {
        public MockHttpServerPlugin(HttpServer server)
        {
            Server = server;
        }

        public Task InitializeAsync(Dictionary<string, string> environmentVars)
        {
            Server.Start();
            return Task.CompletedTask;
        }

        public Task DisposeAsync()
        {
            if (Server != null)
            {
                Server.Stop();
                Server.Dispose();
            }
            return Task.CompletedTask;
        }

        public HttpServer Server { get; private set; }
    }
}
