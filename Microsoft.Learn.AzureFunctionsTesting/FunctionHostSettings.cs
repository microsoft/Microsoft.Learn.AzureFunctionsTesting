using System;
using System.IO;

namespace Microsoft.Learn.AzureFunctionsTesting
{
    public class FunctionHostSettings
    {
        public static FunctionHostSettings Load(string functionAppPath)
        {
            var configuredHostPath = Environment.GetEnvironmentVariable("FunctionsHostExePath");
            if (string.IsNullOrWhiteSpace(configuredHostPath))
            {
                throw new InvalidOperationException(
                    "Set FunctionsHostExePath to the absolute path of a trusted Azure Functions Core Tools executable.");
            }

            var hostPath = Environment.ExpandEnvironmentVariables(configuredHostPath);
            if (!Path.IsPathFullyQualified(hostPath))
            {
                throw new InvalidOperationException("FunctionsHostExePath must be an absolute path.");
            }

            if (!File.Exists(hostPath))
            {
                throw new FileNotFoundException("FunctionsHostExePath does not point to an existing file.", hostPath);
            }

            var functionApplicationPath = Environment.GetEnvironmentVariable("FunctionApplicationPath");
            return new FunctionHostSettings
            {
                FunctionApplicationPath = string.IsNullOrWhiteSpace(functionApplicationPath)
                    ? functionAppPath
                    : functionApplicationPath,
                FunctionsHostExePath = hostPath
            };
        }

        public string? FunctionsHostExePath { get; set; }

        public string? FunctionApplicationPath { get; set; }
    }
}
