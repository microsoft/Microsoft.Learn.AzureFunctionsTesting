using System;
using System.IO;
using System.Security.Cryptography;

namespace Microsoft.Learn.AzureFunctionsTesting
{
    public class FunctionHostSettings
    {
        private readonly byte[] expectedHostHash;

        private FunctionHostSettings(byte[] expectedHostHash)
        {
            this.expectedHostHash = expectedHostHash;
        }

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

            var configuredHash = Environment.GetEnvironmentVariable("FunctionsHostExeSha256");
            if (string.IsNullOrWhiteSpace(configuredHash))
            {
                throw new InvalidOperationException("Set FunctionsHostExeSha256 to the approved SHA-256 digest of the Azure Functions Core Tools executable.");
            }

            if (configuredHash.Length != 64)
            {
                throw new InvalidOperationException("FunctionsHostExeSha256 must be a 64-character hexadecimal SHA-256 digest.");
            }

            byte[] expectedHostHash;
            try
            {
                expectedHostHash = Convert.FromHexString(configuredHash);
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException("FunctionsHostExeSha256 must be a 64-character hexadecimal SHA-256 digest.", ex);
            }

            var functionApplicationPath = Environment.GetEnvironmentVariable("FunctionApplicationPath");
            var settings = new FunctionHostSettings(expectedHostHash)
            {
                FunctionApplicationPath = string.IsNullOrWhiteSpace(functionApplicationPath)
                    ? functionAppPath
                    : functionApplicationPath,
                FunctionsHostExePath = hostPath
            };
            settings.VerifyHostExecutable();
            return settings;
        }

        internal void VerifyHostExecutable()
        {
            using var hostFile = File.OpenRead(FunctionsHostExePath!);
            if (!CryptographicOperations.FixedTimeEquals(SHA256.HashData(hostFile), expectedHostHash))
            {
                throw new InvalidOperationException($"FunctionsHostExePath does not match the approved FunctionsHostExeSha256 digest: '{FunctionsHostExePath}'.");
            }
        }

        public string? FunctionsHostExePath { get; set; }

        public string? FunctionApplicationPath { get; set; }
    }
}
