using Microsoft.Learn.AzureFunctionsTesting.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.Learn.AzureFunctionsTesting.Extension.MockSql
{
    public class MockSqlPlugin : IFunctionTestPlugin
    {
        internal const string DefaultName = "MOCK_SQL_DEFAULT_NAME";

        private readonly List<string> sqlInitFiles = new();
        private readonly List<string> sqlDacPacFiles = new();

        public MockSqlPlugin(string name)
        {
            this.Name = name;
            Database = new LocalDatabase(Guid.NewGuid().ToString());
        }

        public Task InitializeAsync(Dictionary<string, string> environmentVars)
        {
            foreach (var filePath in sqlDacPacFiles)
            {
                Database.DeployDacPac(Path.Combine(Environment.CurrentDirectory, filePath));
            }

            foreach (var filePath in sqlInitFiles)
            {
                Database.RunSqlScriptFile(Path.Combine(Environment.CurrentDirectory, filePath));
            }

            return Task.CompletedTask;
        }

        public Task DisposeAsync()
        {
            Database.Dispose();
            return Task.CompletedTask;
        }

        public string Name { get; private set; }

        public LocalDatabase Database { get; private set; }

        internal void AddSqlScript(string scriptPath) => sqlInitFiles.Add(scriptPath);

        internal void AddDacPac(string scriptPath) => sqlDacPacFiles.Add(scriptPath);
    }
}