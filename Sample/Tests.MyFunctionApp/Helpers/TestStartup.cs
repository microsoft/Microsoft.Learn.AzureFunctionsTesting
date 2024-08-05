using Microsoft.Azure.Cosmos;
using Microsoft.Learn.AzureFunctionsTesting;
using Microsoft.Learn.AzureFunctionsTesting.Extension.DebugProcess;
using Microsoft.Learn.AzureFunctionsTesting.Extension.MockCosmos;
using Microsoft.Learn.AzureFunctionsTesting.Extension.MockHttpServer;
using Microsoft.Learn.AzureFunctionsTesting.Extension.MockSql;
using System;
using System.Threading.Tasks;
using Tests.MyFunctionApp.Helpers;
using Xunit;

// This is required to let the VS test runner know how to find the test framework
[assembly: TestFramework("Microsoft.Learn.AzureFunctionsTesting.TestFramework", "Microsoft.Learn.AzureFunctionsTesting")]
[assembly: AssemblyFixture(typeof(FunctionFixture<TestStartup>))]

namespace Tests.MyFunctionApp.Helpers
{
    public class TestStartup : IFunctionTestStartup
    {
        public void Configure(FunctionTestConfigurationBuilder builder)
        {
            var buildConfig = "Debug";
#if RELEASE
            buildConfig = "Release";
#endif

            // This is required - set it to the relative path to your function app's compiled .dll folder
            builder.SetFunctionAppPath($"..\\..\\..\\..\\MyFunctionApp\\bin\\{buildConfig}\\net8.0");

            // [Optional] You can set these values if necessary
            //builder.SetStartupTimeout(180);
            //builder.SetFunctionAppPort(7081);

            // -- Set up any mocks or other test run configuration as required --

            // mock any HTTP server using a custom class
            var emailService = builder.UseMockServer<EmailServer>("emailServer");

            // mock any HTTP server using a delegate function
            var otherServer = builder.UseMockServer("otherServer", (req, res) =>
            {
                var obj = new { id = "abc", name = "test obj", timestamp = DateTimeOffset.UtcNow };
                res.FromJson(obj);
            });

            // mock Cosmos DB and create the database and collections
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

            // mock SQL Server and create the objects and seed initial data
            var sqlConnectionString = builder.UseSqlServer().WithSqlScript(@"Helpers\setup.sql").ConnectionString;

            // Write your own extension methods
            builder.UseSomething();

            // Do other custom setup/teardown not provided by extensions
            builder.UseCustomAction(() =>
            {
                return Task.CompletedTask;
            }, () =>
            {
                return Task.CompletedTask;
            });

            // pass in any config that your function app uses (things that would normally come from App Settings or local.settings.json, etc)
            builder.ConfigureEnvironmentVariables(env =>
            {
                env.Add("EmailServiceUrl", emailService.Url);
                env.Add("CosmosDbUrl", cosmosInfo.Url.ToString());
                env.Add("CosmosDbKey", cosmosInfo.Key);
                env.Add("SqlConnectionString", sqlConnectionString);
            });

            // allow debugged tests to step into the actual functions code (cross-process)
            builder.DebugIntoFunctions();
        }
    }
}
