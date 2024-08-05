using Microsoft.Learn.AzureFunctionsTesting.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Learn.AzureFunctionsTesting
{
    internal class CustomActionPlugin : IFunctionTestPlugin
    {
        private readonly Func<Task> setup;
        private readonly Func<Task> cleanup;

        public CustomActionPlugin(Func<Task> setup, Func<Task> cleanup)
        {
            this.setup = setup;
            this.cleanup = cleanup;
        }

        public Task InitializeAsync(Dictionary<string, string> environmentVars)
        {
            return setup?.Invoke() ?? Task.CompletedTask;
        }

        public Task DisposeAsync()
        {
            return cleanup?.Invoke() ?? Task.CompletedTask;
        }
    }

    public static class FunctionTestConfigurationBuilderExtensions
    {
        public static void UseCustomAction(this IFunctionTestConfigurationBuilder builder, Func<Task> setup, Func<Task> cleanup)
        {
            var plugin = new CustomActionPlugin(setup, cleanup);
            builder.RegisterPlugin(plugin, $"CUSTOM_ACTION_{Guid.NewGuid()}");
        }
    }
}
