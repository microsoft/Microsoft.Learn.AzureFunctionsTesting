using Microsoft.Learn.AzureFunctionsTesting.Core;
using Microsoft.Learn.AzureFunctionsTesting.Extension.MockHttpServer;

namespace Tests.MyFunctionApp.Helpers
{
    internal static class FunctionTestConfigurationBuilderExtensions
    {
        // You can build your own extension methods via composition as well (no need for a custom plugin)
        internal static void UseSomething(this IFunctionTestConfigurationBuilder builder)
        {
            var server = builder.UseMockServer("something", (req, res) =>
            {

            });
            builder.ConfigureEnvironmentVariables(envVars =>
            {
                envVars.Add("SomethingUrl", server.Url);
            });
        }
    }
}
