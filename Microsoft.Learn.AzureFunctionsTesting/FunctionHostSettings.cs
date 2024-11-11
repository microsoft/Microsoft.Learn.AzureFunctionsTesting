using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.Learn.AzureFunctionsTesting
{
    public class FunctionHostSettings
    {
        private static FunctionHostSettings? instance;

        private FunctionHostSettings() { }

        public static FunctionHostSettings Load(string functionAppPath)
        {
            if (instance == null)
            {
                var functionsHostExePath = string.Empty;
                var pathsToTry = new List<string>();
                var azureFunctionsMajorVersion = "4";

                // First, try the runtime installation directory and find the latest version (used by devs on their local machines)
                var toolsDir = Environment.ExpandEnvironmentVariables("%LOCALAPPDATA%\\AzureFunctionsTools\\Releases");
                if (Directory.Exists(toolsDir))
                {
                    var toolsDirInfo = new DirectoryInfo(toolsDir);
                    var latestToolsDirInfo = toolsDirInfo.EnumerateDirectories().OrderByDescending(d => d.LastWriteTimeUtc).FirstOrDefault(d => d.Name.StartsWith(azureFunctionsMajorVersion));
                    if (latestToolsDirInfo != null)
                    {
                        pathsToTry.Add(Path.Combine(latestToolsDirInfo.FullName, "cli", "func.exe"));
                        pathsToTry.Add(Path.Combine(latestToolsDirInfo.FullName, "cli_x64", "func.exe"));
                    }
                }

                // global npm folder for local devs
                pathsToTry.Add(Environment.ExpandEnvironmentVariables("%APPDATA%\\npm\\node_modules\\azure-functions-core-tools\\bin\\func.exe"));

                // node_modules folder in this repository for CI builds
                var workingDirectory = Environment.GetEnvironmentVariable("SYSTEM_DEFAULTWORKINGDIRECTORY");
                if (!string.IsNullOrEmpty(workingDirectory)) pathsToTry.Add($"{workingDirectory}\\node_modules\\azure-functions-core-tools_{azureFunctionsMajorVersion}\\bin\\func.exe");

                // global npm folder for CI builds
                pathsToTry.Add("C:\\npm\\prefix\\node_modules\\azure-functions-core-tools\\bin\\func.exe");

                // homebrew folder for mac
                pathsToTry.Add("/opt/homebrew/bin/func");

                foreach (var pathToTry in pathsToTry)
                {
                    if (File.Exists(pathToTry))
                    {
                        functionsHostExePath = pathToTry;
                        break;
                    }
                }

                var configurationRoot = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        { "FunctionsHostExePath", functionsHostExePath },
                        { "FunctionApplicationPath", functionAppPath }
                    })
                    .AddEnvironmentVariables()
                    .Build();
                instance = new FunctionHostSettings();
                configurationRoot.Bind(instance);
            }
            return instance;
        }

        public string? FunctionsHostExePath { get; set; }

        public string? FunctionApplicationPath { get; set; }
    }
}
