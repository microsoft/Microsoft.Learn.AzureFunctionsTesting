using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Learn.AzureFunctionsTesting.Extension.DebugProcess.Core;
using System;

// put this as the first line of code (will be a no-op when not debugging tests)
DebugHelper.WaitForDebuggerToAttach();

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((hbc, services) =>
    {
        var config = hbc.Configuration;
        services.AddSingleton(new CosmosClient(config["CosmosDbUrl"], config["CosmosDbKey"]));
        services.AddHttpClient("EmailService", configure =>
        {
            configure.BaseAddress = new Uri(config["EmailServiceUrl"]);
        });
    })
    .Build();

host.Run();
