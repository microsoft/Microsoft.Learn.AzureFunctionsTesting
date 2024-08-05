using Microsoft.Learn.AzureFunctionsTesting.Core;

namespace Microsoft.Learn.AzureFunctionsTesting.Extension.MockHttpServer
{
    public static class FunctionFixtureExtensions
    {
        public static HttpServer? GetHttpServer(this IFunctionFixture fixture, string name)
        {
            var plugin = fixture.GetPlugin<MockHttpServerPlugin>(name);
            return plugin?.Server;
        }
    }
}
