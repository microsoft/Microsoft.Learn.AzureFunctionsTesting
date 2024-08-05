using Microsoft.Azure.Cosmos;
using Microsoft.Learn.AzureFunctionsTesting.Core;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Learn.AzureFunctionsTesting.Extension.MockCosmos
{
    public class MockCosmosPlugin : IFunctionTestPlugin
    {
        const string defaultUrl = "https://localhost:8081";
        internal const string WellKnownEmulatorKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
        internal const string DefaultName = "MOCK_COSMOS_DEFAULT_NAME";

        private readonly Func<CosmosClient, Task> dbSetup;
        private readonly Func<CosmosClient, Task> dbCleanup;

        public MockCosmosPlugin(string name, Func<CosmosClient, Task> dbSetup, Func<CosmosClient, Task> dbCleanup, CosmosSerializationOptions? cosmosSerializationOptions = null)
        {
            this.Name = name;
            this.Url = defaultUrl;
            this.dbSetup = dbSetup;
            this.dbCleanup = dbCleanup;

            this.Client = new CosmosClient(Url, WellKnownEmulatorKey, new CosmosClientOptions
            {
                ConnectionMode = ConnectionMode.Direct,
                SerializerOptions = cosmosSerializationOptions
            });
        }

        public string Name { get; private set; }

        public string Url { get; private set; }

        public CosmosClient Client { get; private set; }

        public async Task InitializeAsync(Dictionary<string, string> environmentVars)
        {
            var emulatorTestClient = new HttpClient();
            try
            {
                var response = await emulatorTestClient.GetAsync(Url);
            }
            catch
            {
                throw new Exception("Cosmos DB emulator is not running");
            }

            await (dbSetup?.Invoke(Client) ?? Task.CompletedTask);
        }

        public async Task DisposeAsync()
        {
            await (dbCleanup?.Invoke(Client) ?? Task.CompletedTask);
        }
    }
}