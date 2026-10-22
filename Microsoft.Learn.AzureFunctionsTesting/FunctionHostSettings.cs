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
                var settings = new FunctionHostSettings
                {
                    FunctionApplicationPath = Environment.GetEnvironmentVariable("FunctionApplicationPath"),
                    FunctionsHostExePath = Environment.GetEnvironmentVariable("FunctionsHostExePath")
                };
                if (string.IsNullOrWhiteSpace(settings.FunctionApplicationPath))
                {
                    settings.FunctionApplicationPath = functionAppPath;
                }

                settings.FunctionsHostExePath = ResolveHostExecutablePath(
                    settings.FunctionsHostExePath,
                    GetHostExecutableCandidates(
                        settings.FunctionApplicationPath!,
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        OperatingSystem.IsWindows(),
                        OperatingSystem.IsMacOS()));
                instance = settings;
            }
            return instance;
        }

        internal static IEnumerable<string> GetHostExecutableCandidates(
            string functionAppPath,
            string localApplicationData,
            string applicationData,
            bool isWindows,
            bool isMacOS)
        {
            if (isWindows)
            {
                // First, try the runtime installation directory and find the latest version (used by devs on their local machines)
                // Note that newer versions of the Azure Core Tools have different folders for in-process vs isolated func.exe versions
                if (!string.IsNullOrEmpty(localApplicationData))
                {
                    var fullPath = Path.Combine(Directory.GetCurrentDirectory(), functionAppPath);
                    var isInProcess = !File.Exists(Path.Combine(fullPath, "extensions.json"));
                    var toolsDir = Path.Combine(localApplicationData, "AzureFunctionsTools", "Releases");
                    if (Directory.Exists(toolsDir))
                    {
                        var toolsDirInfo = new DirectoryInfo(toolsDir);
                        var latestToolsDirInfo = toolsDirInfo.EnumerateDirectories()
                            .Where(d => d.Name.StartsWith("4"))
                            .Where(d => isInProcess || !d.Name.EndsWith("inprocess"))
                            .OrderByDescending(d => d.LastWriteTimeUtc)
                            .FirstOrDefault();
                        if (latestToolsDirInfo != null)
                        {
                            yield return Path.Combine(latestToolsDirInfo.FullName, "cli", "func.exe");
                            yield return Path.Combine(latestToolsDirInfo.FullName, "cli_x64", "func.exe");
                        }
                    }
                }

                // global npm folder for local devs
                if (!string.IsNullOrEmpty(applicationData))
                {
                    yield return Path.Combine(applicationData, "npm", "node_modules", "azure-functions-core-tools", "bin", "func.exe");
                }
            }
            else if (isMacOS)
            {
                yield return "/opt/homebrew/bin/func";
            }
        }

        internal static string ResolveHostExecutablePath(string? configuredPath, IEnumerable<string> candidates)
        {
            if (!string.IsNullOrEmpty(configuredPath))
            {
                var expandedPath = Environment.ExpandEnvironmentVariables(configuredPath);
                if (!Path.IsPathFullyQualified(expandedPath))
                {
                    throw new InvalidOperationException("FunctionsHostExePath must be a fully qualified absolute path to the Azure Functions Core Tools executable.");
                }
                if (!File.Exists(expandedPath))
                {
                    throw new InvalidOperationException($"Invalid FunctionsHostExePath - '{expandedPath}' does not exist. Set FunctionsHostExePath to the absolute path of an installed Azure Functions Core Tools executable.");
                }
                return expandedPath;
            }

            foreach (var candidate in candidates)
            {
                if (Path.IsPathFullyQualified(candidate) && File.Exists(candidate))
                {
                    return candidate;
                }
            }

            throw new InvalidOperationException("Azure Functions Core Tools could not be found. Install Core Tools using Visual Studio or per-user npm on Windows, or Homebrew on macOS, or set FunctionsHostExePath to the absolute path of the executable. CI and other installation locations require FunctionsHostExePath.");
        }

        public string? FunctionsHostExePath { get; set; }

        public string? FunctionApplicationPath { get; set; }
    }
}
