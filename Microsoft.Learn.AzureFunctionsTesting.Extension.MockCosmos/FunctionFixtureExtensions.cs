using Microsoft.Azure.Cosmos;
using Microsoft.Learn.AzureFunctionsTesting.Core;

namespace Microsoft.Learn.AzureFunctionsTesting.Extension.MockCosmos
{
    public static class FunctionFixtureExtensions
    {
        public static CosmosClient? GetCosmos(this IFunctionFixture fixture)
        {
            return fixture.GetCosmos(MockCosmosPlugin.DefaultName);
        }

        public static CosmosClient? GetCosmos(this IFunctionFixture fixture, string name)
        {
            var plugin = fixture.GetPlugin<MockCosmosPlugin>(name);
            return plugin?.Client;
        }
    }
}
