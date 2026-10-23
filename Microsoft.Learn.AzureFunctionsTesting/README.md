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

- [`Microsoft.Learn.AzureFunctionsTesting.Extension.MockHttpServer`](../Microsoft.Learn.AzureFunctionsTesting.Extension.MockHttpServer/README.md)

- [`Microsoft.Learn.AzureFunctionsTesting.Extension.MockCosmos`](../Microsoft.Learn.AzureFunctionsTesting.Extension.MockCosmos/README.md)

- [`Microsoft.Learn.AzureFunctionsTesting.Extension.MockSql`](../Microsoft.Learn.AzureFunctionsTesting.Extension.MockSql/README.md)

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
                //builder.DisableFunctionsAuth();

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
The solution is the `Microsoft.Learn.AzureFunctionsTesting.Extension.DebugProcess` package. [See that README for more information](../Microsoft.Learn.AzureFunctionsTesting.Extension.DebugProcess/README.md).

## Configuring for CI/CD

All of the functionality works the same way when run in an Azure DevOps pipeline. Set the function app path for your build configuration and the Functions host executable path for your installation.
The function app path can be set with the following environment variable:

	variables:
	  buildConfiguration: 'Release'
	  FunctionApplicationPath: '..\\..\\..\\..\\..\\Your.App\\bin\\Release\\net8.0'	# Note that this is using the Release build

In order for the Functions host runtime to work in a DevOps pipeline, install an approved, exact version of Azure Functions Core Tools before running tests. The [package pipeline](../azure-pipelines-pack.yaml) downloads the pinned Windows release, checks the archive against a reviewed digest before extracting it, verifies the executable, and sets `FunctionsHostExePath` for the test task.

**New in 1.3.0:** the host is started with `--address`, which defaults to `0.0.0.0` - the same address Core Tools bound to previously, so no changes are required. Core Tools versions before 4.14.0 ignore the flag. Core Tools 4.14.0 changes its own default bind address to the IPv4 loopback address, but because the fixture always passes `--address`, the host remains reachable from other machines. Use `SetFunctionAppAddress` if you want to restrict that - for example, `SetFunctionAppAddress("127.0.0.1")` to bind to loopback only.

**Breaking change in 2.0.0:** set `FunctionsHostExePath` to the absolute path of a trusted Core Tools v4 executable before running integration tests. The package no longer searches installed locations or requires `FunctionsHostExeSha256`. A missing, relative, or nonexistent path fails without falling back to another installation:

	variables:
	  FunctionsHostExePath: '<absolute-path-to-trusted-func>'

For local development, you can set the path yourself or run the optional [PowerShell helper](https://github.com/microsoft/Microsoft.Learn.AzureFunctionsTesting/blob/main/scripts/Set-LocalFunctionsHostPath.ps1) from a source checkout (it is also included in the NuGet package's `tools` folder). It finds a Visual Studio, global npm, or Homebrew installation **only when you request that source**, displays the selected path, and does not download or verify Core Tools:

	.\scripts\Set-LocalFunctionsHostPath.ps1 -Source VisualStudio
	# Or: -Source Npm (Windows/macOS/Linux), or -Source Homebrew (macOS)

Run your tests from the same PowerShell session. To use the Visual Studio test runner, run `.\scripts\Set-LocalFunctionsHostPath.ps1 -Source VisualStudio -Scope User` on Windows and restart Visual Studio so it reads the new user environment variable. If Visual Studio changes its Core Tools installation, rerun the helper and restart Visual Studio. When Visual Studio has several v4 installations, choose the one appropriate for your app from the displayed list. For npm or Homebrew, trust the package manager, installation location, and their permissions before opting in; the helper will reject the shared `C:\npm\prefix` location.

The package validates the explicit path, **not** the executable's signature, hash, or ownership. An absolute path does not stop an attacker who can replace that file or its dependencies. Install Core Tools from a trusted source and protect the installation and test environment. CI should provision and verify its own version as in the package pipeline; do not use the local helper in CI or expose secrets to untrusted build steps.

Note that in order to use the CosmosDb emulator in a DevOps pipeline, you also have to add the required task to enable the emulator before the tests task:

    - task: CosmosDbEmulator@2
      inputs:
        containerName: 'azure-cosmosdb-emulator'
        enableAPI: 'SQL'
        portMapping: '8081:8081, 8901:8901, 8902:8902, 8979:8979, 10250:10250, 10251:10251, 10252:10252, 10253:10253, 10254:10254, 10255:10255, 10256:10256, 10350:10350'
        hostDirectory: '$(Build.BinariesDirectory)\azure-cosmosdb-emulator'

## Authoring extensions

Information about how to author your own extensions [can be found in the Microsoft.Learn.AzureFunctionsTesting.Core README](../Microsoft.Learn.AzureFunctionsTesting.Core/README.md).