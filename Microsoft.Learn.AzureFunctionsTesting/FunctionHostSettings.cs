using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.Learn.AzureFunctionsTesting
{
    public class FunctionHostSettings
    {
        public static FunctionHostSettings Load(string functionAppPath)
        {
            var configuredAppPath = Environment.GetEnvironmentVariable("FunctionApplicationPath");
            var functionApplicationPath = string.IsNullOrWhiteSpace(configuredAppPath)
                ? functionAppPath
                : configuredAppPath;
            var configuredHostPath = Environment.GetEnvironmentVariable("FunctionsHostExePath");
            var hostPath = string.IsNullOrWhiteSpace(configuredHostPath)
                ? FindDeveloperHostPath(functionApplicationPath)
                : Environment.ExpandEnvironmentVariables(configuredHostPath);
            if (string.IsNullOrWhiteSpace(hostPath))
            {
                throw new InvalidOperationException(
                    "Azure Functions Core Tools was not found in a per-user Visual Studio or npm installation. " +
                    "Set FunctionsHostExePath to the absolute path of a trusted Core Tools v4 executable. " +
                    "Shared installations, CI workspaces, and Homebrew are not searched automatically.");
            }

            if (!Path.IsPathFullyQualified(hostPath))
            {
                throw new InvalidOperationException("FunctionsHostExePath must be an absolute path.");
            }

            if (!File.Exists(hostPath))
            {
                throw new FileNotFoundException("FunctionsHostExePath does not point to an existing file.", hostPath);
            }

            return new FunctionHostSettings
            {
                FunctionApplicationPath = functionApplicationPath,
                FunctionsHostExePath = hostPath
            };
        }

        private static string? FindDeveloperHostPath(string functionApplicationPath)
        {
            if (!OperatingSystem.IsWindows())
            {
                return null;
            }

            var pathsToTry = new List<string>();
            var localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
            if (Path.IsPathFullyQualified(localAppData ?? string.Empty))
            {
                var toolsDirectory = Path.Combine(localAppData!, "AzureFunctionsTools", "Releases");
                if (Directory.Exists(toolsDirectory))
                {
                    var isInProcess = !File.Exists(Path.Combine(functionApplicationPath, "extensions.json"));
                    var latestToolsDirectory = new DirectoryInfo(toolsDirectory).EnumerateDirectories()
                        .Where(d => d.Name == "4" || d.Name.StartsWith("4.", StringComparison.Ordinal))
                        .Where(d => isInProcess || !d.Name.EndsWith("inprocess", StringComparison.OrdinalIgnoreCase))
                        .OrderByDescending(d => d.LastWriteTimeUtc)
                        .FirstOrDefault();
                    if (latestToolsDirectory != null)
                    {
                        pathsToTry.Add(Path.Combine(latestToolsDirectory.FullName, "cli", "func.exe"));
                        pathsToTry.Add(Path.Combine(latestToolsDirectory.FullName, "cli_x64", "func.exe"));
                    }
                }
            }

            var appData = Environment.GetEnvironmentVariable("APPDATA");
            if (Path.IsPathFullyQualified(appData ?? string.Empty))
            {
                pathsToTry.Add(Path.Combine(appData!, "npm", "node_modules", "azure-functions-core-tools", "bin", "func.exe"));
            }

            return pathsToTry.FirstOrDefault(File.Exists);
        }

        public string? FunctionsHostExePath { get; set; }

        public string? FunctionApplicationPath { get; set; }
    }
}
