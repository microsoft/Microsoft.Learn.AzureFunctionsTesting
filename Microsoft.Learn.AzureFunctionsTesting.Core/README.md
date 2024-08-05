# Microsoft.Learn.AzureFunctionsTesting.Core

This package contains the core interfaces used by extension authors to create plugins for the testing system.

## Creating a extension

There are three steps to creating an extension:

1. Create a class that implements `IFunctionTestPlugin`
2. Create an extension method for `IFunctionTestConfigurationBuilder` where you register your plugin
3. [Optional] Create an extension method for `IFunctionFixture` that lets you access the plugin from tests

### Creating the plugin

The `InitializeAsync()` method will get called once when the test run starts and you should use it to do any test suite setup.
The `DisposeAsync()` method will get called once when the test run completes and you can use it ot do any test suite teardown.
You can expose a property (ex: CosmosClient, Database, etc) that you can get back to from individual tests to do any per-test setup.

### Registering your plugin

Create an extension method on `IFunctionTestConfigurationBuilder`. You **must** call `builder.RegisterPlugin()` in order for your plugin to get called by the test runtime.
You can also optionally register any environment variables which will get converted into `IConfiguration` values that can be accessed from your function app.

### Use your plugin in tests

Create an extension method on `IFunctionFixture` and call `fixture.GetPlugin()` to get a reference to your plugin. Use the same name you used when registering the plugin.
You can access the custom property on your plugin to do per-test arrangement (inserting database records, etc).

