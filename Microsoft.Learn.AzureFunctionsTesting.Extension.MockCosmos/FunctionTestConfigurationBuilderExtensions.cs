using Microsoft.Azure.Cosmos;
using Microsoft.Learn.AzureFunctionsTesting.Core;
using System;
using System.Threading.Tasks;

namespace Microsoft.Learn.AzureFunctionsTesting.Extension.MockCosmos
{
    public static class FunctionTestConfigurationBuilderExtensions
    {
        public static CosmosDbInfo UseCosmosDbEmulator(this IFunctionTestConfigurationBuilder builder, Func<CosmosClient, Task> dbSetup, Func<CosmosClient, Task> dbCleanup, CosmosSerializationOptions? cosmosSerializationOptions = null)
        {
            return builder.UseCosmosDbEmulator(MockCosmosPlugin.DefaultName, dbSetup, dbCleanup, cosmosSerializationOptions);
        }

        public static CosmosDbInfo UseCosmosDbEmulator(this IFunctionTestConfigurationBuilder builder, string name, Func<CosmosClient, Task> dbSetup, Func<CosmosClient, Task> dbCleanup, CosmosSerializationOptions? cosmosSerializationOptions = null)
        {
            var plugin = new MockCosmosPlugin(name, dbSetup, dbCleanup, cosmosSerializationOptions);
            builder.RegisterPlugin(plugin, plugin.Name);
            return new CosmosDbInfo(plugin.Url, MockCosmosPlugin.WellKnownEmulatorKey);
        }
    }
}
