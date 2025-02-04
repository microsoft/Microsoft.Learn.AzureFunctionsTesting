using Microsoft.Learn.AzureFunctionsTesting.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Learn.AzureFunctionsTesting
{
    public class FunctionTestConfigurationBuilder : IFunctionTestConfigurationBuilder
    {
        private readonly List<IFunctionTestPlugin> plugins = new();
        private readonly Dictionary<string, IFunctionTestPlugin> pluginMap = new();

        internal FunctionTestConfigurationBuilder()
        {
        }

        internal string? FunctionAppPath { get; set; } = null;

        internal bool EnableAuth { get; set; } = true;

        internal int Port { get; set; } = 7071;

        internal int StartupTimeout { get; set; } = 15;

        internal List<Action<Dictionary<string, string>>> ConfigureEnvironmentVariablesActions { get; } = new();

        internal List<Func<Dictionary<string, string>, Task>> BeforeProcessStartActions { get; } = new();

        internal List<Func<int, Task>> AfterProcessStartActions { get; } = new();


        internal List<IFunctionTestPlugin> GetPlugins() => plugins;

        internal T? GetPlugin<T>(string name) where T : IFunctionTestPlugin
        {
            var key = GetPluginMapKey(name, typeof(T));
            pluginMap.TryGetValue(key, out var plugin);
            return (T?)plugin;
        }

        public void DisableFunctionsAuth()
        {
            this.EnableAuth = false;
        }

        public void SetFunctionAppPath(string path)
        {
            this.FunctionAppPath = path;
        }

        public void SetFunctionAppPort(int port)
        {
            this.Port = port;
        }

        public void SetStartupTimeout(int seconds)
        {
            this.StartupTimeout = seconds;
        }

        public void RegisterPlugin(IFunctionTestPlugin plugin, string name)
        {
            plugins.Add(plugin);
            var key = GetPluginMapKey(name, plugin.GetType());
            pluginMap[key] = plugin;
        }

        public void ConfigureEnvironmentVariables(Action<Dictionary<string, string>> configure)
        {
            this.ConfigureEnvironmentVariablesActions.Add(configure);
        }

        public void BeforeProcessStart(Func<Dictionary<string, string>, Task> action)
        {
            this.BeforeProcessStartActions.Add(action);
        }

        public void AfterProcessStart(Func<int, Task> action)
        {
            this.AfterProcessStartActions.Add(action);
        }

        static string GetPluginMapKey(string name, Type type)
        {
            return $"{type}-{name}";
        }
    }
}
