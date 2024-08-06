# Azure Functions Integration Testing Framework

This library provides a helpful framework that allows you to test your Azure Functions Http Triggers in a manner similar to how ASP.NET Core integration testing works.

## Usage

1. Add a reference to the `Microsoft.Learn.AzureFunctionsTesting` NuGet package to your integration test project.
2. Add the required assembly attributes and a class that implements `IFunctionTestStartup`. You can copy and paste the code below and then modify as necessary:

		using Microsoft.Azure.Cosmos;
		using Microsoft.Learn.AzureFunctionsTesting;
		using Microsoft.Learn.AzureFunctionsTesting.Extension.DebugProcess;
		using Microsoft.Learn.AzureFunctionsTesting.Extension.MockCosmos;
		using Microsoft.Learn.AzureFunctionsTesting.Extension.MockHttpServer;
		using Microsoft.Learn.AzureFunctionsTesting.Extension.MockSql;
		using System;
		using System.Threading.Tasks;
		using Xunit;

		// NOTE: Do not change this value
		[assembly: TestFramework("Microsoft.Learn.AzureFunctionsTesting.TestFramework", "Microsoft.Learn.AzureFunctionsTesting")]
		// NOTE: Where <T> is the class that implements your test configuration logic
		[assembly: AssemblyFixture(typeof(FunctionFixture<TestStartup>))]

		namespace Your.Namespace
		{
			public class TestStartup : IFunctionTestStartup
			{
				public void Configure(FunctionTestConfigurationBuilder builder)
				{
					// This should be the relative path from where the test .dll is built to your function app .dll
					// Make sure the app folder name is correct, as well as the version
					builder.SetFunctionAppPath("..\\..\\..\\..\\..\\Your.App\\bin\\Debug\\net8.0");
				}
			}
		}

3. In your test classes, add a constructor that takes a `FunctionFixture<T>` parameter:

        public ApiTests(FunctionFixture<TestStartup> fixture)
        {
            this.fixture = fixture;
        }

4. Use the `HttpClient` provided by `fixture.Client` to make calls to your HttpTrigger functions:

		[Fact]
		public Task Test_It()
		{
			var response = await fixture.Client.GetAsync("/api/whatever");
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		}

Note that the above code is the minimum needed, but there are additional helper methods you can use:

- `builder.ConfigureEnvironmentVariables(dictionary)` - use this to set the key-value pairs that should be passed to your Functions app. Useful for passing in custom values during CI builds.

- `builder.UseCustomAction(setup, cleanup)` - use this to run any other additional setup/cleanup logic

Additionally, you can reference extension packages that provide mocks for other commonly used dependencies:

- [`Microsoft.Learn.AzureFunctionsTesting.Extension.MockHttpServer`](Microsoft.Learn.AzureFunctionsTesting.Extension.MockHttpServer/README.md)

- [`Microsoft.Learn.AzureFunctionsTesting.Extension.MockCosmos`](Microsoft.Learn.AzureFunctionsTesting.Extension.MockCosmos/README.md)

- [`Microsoft.Learn.AzureFunctionsTesting.Extension.MockSql`](Microsoft.Learn.AzureFunctionsTesting.Extension.MockSql/README.md)

See below for a complete example:

    using Microsoft.Azure.Cosmos;
    using Microsoft.Learn.AzureFunctionsTesting;
    using Microsoft.Learn.AzureFunctionsTesting.Extension.DebugProcess;
    using Microsoft.Learn.AzureFunctionsTesting.Extension.MockCosmos;
    using Microsoft.Learn.AzureFunctionsTesting.Extension.MockHttpServer;
    using Microsoft.Learn.AzureFunctionsTesting.Extension.MockSql;
    using System;
    using System.Threading.Tasks;
    using Xunit;

    // This is required to let the VS test runner know how to find the test framework
    [assembly: TestFramework("Microsoft.Learn.AzureFunctionsTesting.TestFramework", "Microsoft.Learn.AzureFunctionsTesting")]
    [assembly: AssemblyFixture(typeof(FunctionFixture<TestStartup>))]

    namespace Your.Namespace
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

                // Set up any mocks or other test run configuration as required
                var emailService = builder.UseMockServer<EmailServer>("emailServer");

                var otherServer = builder.UseMockServer("otherServer", (req, res) =>
                {
                    var obj = new { id = "abc", name = "test obj", timestamp = DateTimeOffset.UtcNow };
                    res.FromJson(obj);
                });

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

## Debugging

Since these are integration tests, they test the code like a client would by making requests and inspecting the result. However, it is often useful to be able to 
see what the server is doing and step through the code. Since the tests and the functions host run in two (or three) different processes, that can be tricky.
The solution is the `Microsoft.Learn.AzureFunctionsTesting.Extension.DebugProcess` package. [See that README for more information](Microsoft.Learn.AzureFunctionsTesting.Extension.DebugProcess/README.md).

**Note:** At this time the only supported platform for the debugging functionality is Windows.

## Configuring for CI/CD

All of the functionality works the same way when ran in an Azure DevOps pipeline. The only difference is the configuration of the function app path.
That value can be set with the following environment variables:

	variables:
	  buildConfiguration: 'Release'
	  FunctionApplicationPath: '..\\..\\..\\..\\..\\Your.App\\bin\\Release\\net8.0'	# Note that this is using the Release build

In order for the Functions host runtime to work in a DevOps pipeline, the Azure Functions Core tools must be installed. Ensure that the following task is added before the task that runs the test:

    - task: Npm@1
      displayName: Install Azure Functions Core Tools
      inputs:
        command: custom
        verbose: false
        customCommand: 'install -g azure-functions-core-tools'

Note that by default, these tools are always installed at `C:\\npm\\prefix\\node_modules\\azure-functions-core-tools\\bin\\func.exe` but if for some reason you need to specify a different path, you can override it with a variable called `FunctionsHostExePath`:

	variables:
	  FunctionsHostExePath: 'C:\\npm\\prefix\\node_modules\\azure-functions-core-tools\\bin\\func.exe'

Note that in order to use the CosmosDb emulator in a DevOps pipeline, you also have to add the required task to enable the emulator before the tests task:

    - task: CosmosDbEmulator@2
      inputs:
        containerName: 'azure-cosmosdb-emulator'
        enableAPI: 'SQL'
        portMapping: '8081:8081, 8901:8901, 8902:8902, 8979:8979, 10250:10250, 10251:10251, 10252:10252, 10253:10253, 10254:10254, 10255:10255, 10256:10256, 10350:10350'
        hostDirectory: '$(Build.BinariesDirectory)\azure-cosmosdb-emulator'

## Authoring extensions

Information about how to author your own extensions [can be found in the Microsoft.Learn.AzureFunctionsTesting.Core README](Microsoft.Learn.AzureFunctionsTesting.Core/README.md).

----

## Trademarks 

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft trademarks or logos is subject to and must follow [Microsoft’s Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general). Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship. Any use of third-party trademarks or logos are subject to those third-party’s policies.