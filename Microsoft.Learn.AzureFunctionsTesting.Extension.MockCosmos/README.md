# Azure Functions Integration Testing Framework - Mocking CosmosDB

If your Azure Functions app uses CosmosDB, you can use this package along with the CosmosDB emulator to make mocking easy with no code changes required to your function app code.

## Prerequisites

- [Azure CosmosDB Emulator](https://docs.microsoft.com/en-us/azure/cosmos-db/local-emulator) installed and running

## Setup

In your TestStartup, call `builder.UseCosmosDbEmulator(setup, teardown)`. You can use the `setup` delegate to create your database and any containers, and the `teardown` delegate to do any cleanup.

Example:

    var databaseName = "MyDb";
    var cosmosInfo = builder.UseCosmosDbEmulator(async db =>
    {
        await db.CreateDatabaseIfNotExistsAsync(databaseName);

        var collections = new[] { "Users", "OtherCollection" };
        foreach (var collection in collections)
        {
            ContainerProperties containerProperties;
            if (collection == "Users")
            {
                containerProperties = new ContainerProperties(collection, "/id");
            }
            else
            {
                containerProperties = new ContainerProperties(collection, "/partitionKey");
            }

            await db.GetDatabase(databaseName).CreateContainerIfNotExistsAsync(containerProperties);
        }
    }, async db =>
    {
        await db.GetDatabase(databaseName).DeleteAsync();
    });

If your code connects to multiple Cosmos DBs, you can use the overload that takes a `name` and call `.UseCosmosDbEmulator()` multiple times, once for each database you use.

`.UseCosmosDbEmulator()` returns an object with the URL and key of the emulator, which you can use to configure your function app however you would normally pass in your DB name and key:

	builder.ConfigureEnvironmentVariables(env =>
	{
		env.Add("CosmosDbUrl", cosmosInfo.Url.ToString());
		env.Add("CosmosDbKey", cosmosInfo.Key);
	});

(Note that the emulator does not support managed identity or other types of auth, so the key is required when configuring your `CosmosClient`.)

## Usage

In your tests, you can call `fixture.GetCosmos()` (optionally passing in a `name` if you configured multiple DBs) which will return a `CosmosClient` object that you can use to insert/update/read data as needed.

A convenient approach is to wrap your logic in an extension method like this:

    public static class FixtureExtensions
    {
        const string databaseName = "MyDb";
        const string containerName = "Users";

        public static async Task AddUserToDatabase<T>(this FunctionFixture<T> fixture, string userId, string name) where T : IFunctionTestStartup
        {
            var cosmos = fixture.GetCosmos();
            if (cosmos == null) return;
            var container = cosmos.GetContainer(databaseName, containerName);
            await container.UpsertItemAsync(new User { Id = userId, Name = name }, new PartitionKey(userId));
        }
    }

Then in your tests, you can do stuff like:

	await fixture.AddUserToDatabase("123", "Test User");

Which makes per-test data setup straightforward.

## Other Notes

Note that in order to use the CosmosDb emulator in a DevOps pipeline, you also have to add the required task to enable the emulator before the tests task:

    - task: CosmosDbEmulator@2
      inputs:
        containerName: 'azure-cosmosdb-emulator'
        enableAPI: 'SQL'
        portMapping: '8081:8081, 8901:8901, 8902:8902, 8979:8979, 10250:10250, 10251:10251, 10252:10252, 10253:10253, 10254:10254, 10255:10255, 10256:10256, 10350:10350'
        hostDirectory: '$(Build.BinariesDirectory)\azure-cosmosdb-emulator'
