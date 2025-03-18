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
                // Load the settings from environment variables first - if they are set, we dont need to do the rest of this
                instance = new FunctionHostSettings
                {
                    FunctionApplicationPath = Environment.GetEnvironmentVariable("FunctionApplicationPath"),
                    FunctionsHostExePath = Environment.GetEnvironmentVariable("FunctionsHostExePath")
                };
                if (string.IsNullOrWhiteSpace(instance.FunctionApplicationPath))
                {
                    instance.FunctionApplicationPath = functionAppPath;
                }
                if (!string.IsNullOrEmpty(instance.FunctionsHostExePath))
                {
                    return instance;
                }

                var pathsToTry = new List<string>();
                var azureFunctionsMajorVersion = "4";

                // First, try the runtime installation directory and find the latest version (used by devs on their local machines)
                // Note that newer versions of the Azure Core Tools have different folders for in-process vs isolated func.exe versions
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), instance.FunctionApplicationPath);
                var isInProcess = !File.Exists(Path.Combine(fullPath!, "extensions.json"));
                var toolsDir = Environment.ExpandEnvironmentVariables("%LOCALAPPDATA%\\AzureFunctionsTools\\Releases");
                if (Directory.Exists(toolsDir))
                {
                    var toolsDirInfo = new DirectoryInfo(toolsDir);
                    var latestToolsDirInfo = toolsDirInfo.EnumerateDirectories()
                        .Where(d => d.Name.StartsWith(azureFunctionsMajorVersion))
                        .Where(d => isInProcess || !d.Name.EndsWith("inprocess"))
                        .OrderByDescending(d => d.LastWriteTimeUtc)
                        .FirstOrDefault();
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


                // loop over all of the paths to try and take the first one that actually exists
                foreach (var pathToTry in pathsToTry)
                {
                    if (File.Exists(pathToTry))
                    {
                        instance.FunctionsHostExePath = pathToTry;
                        break;
                    }
                }
            }
            return instance;
        }

        public string? FunctionsHostExePath { get; set; }

        public string? FunctionApplicationPath { get; set; }
    }
}
