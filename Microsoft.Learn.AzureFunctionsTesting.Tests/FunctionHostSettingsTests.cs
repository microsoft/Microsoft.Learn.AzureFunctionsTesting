using System;
using System.IO;
using System.Security.Cryptography;
using Xunit;

namespace Microsoft.Learn.AzureFunctionsTesting.Tests
{
    public class FunctionHostSettingsTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public void Load_requires_an_explicit_host_path(string? configuredPath)
        {
            var originalPath = Environment.GetEnvironmentVariable("FunctionsHostExePath");
            try
            {
                Environment.SetEnvironmentVariable("FunctionsHostExePath", configuredPath);

                var exception = Assert.Throws<InvalidOperationException>(() => FunctionHostSettings.Load("app"));

                Assert.Contains("FunctionsHostExePath", exception.Message);
            }
            finally
            {
                Environment.SetEnvironmentVariable("FunctionsHostExePath", originalPath);
            }
        }

        [Fact]
        public void Load_rejects_relative_host_path()
        {
            var originalPath = Environment.GetEnvironmentVariable("FunctionsHostExePath");
            try
            {
                Environment.SetEnvironmentVariable("FunctionsHostExePath", "node_modules/azure-functions-core-tools/bin/func.exe");

                Assert.Throws<InvalidOperationException>(() => FunctionHostSettings.Load("app"));
            }
            finally
            {
                Environment.SetEnvironmentVariable("FunctionsHostExePath", originalPath);
            }
        }

        [Fact]
        public void Load_rejects_missing_host_file()
        {
            var originalPath = Environment.GetEnvironmentVariable("FunctionsHostExePath");
            try
            {
                Environment.SetEnvironmentVariable("FunctionsHostExePath",
                    Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "func.exe"));

                Assert.Throws<FileNotFoundException>(() => FunctionHostSettings.Load("app"));
            }
            finally
            {
                Environment.SetEnvironmentVariable("FunctionsHostExePath", originalPath);
            }
        }

        [Fact]
        public void Load_uses_configured_existing_host_path()
        {
            var originalHostPath = Environment.GetEnvironmentVariable("FunctionsHostExePath");
            var originalHash = Environment.GetEnvironmentVariable("FunctionsHostExeSha256");
            var originalAppPath = Environment.GetEnvironmentVariable("FunctionApplicationPath");
            var hostPath = Path.GetTempFileName();
            try
            {
                Environment.SetEnvironmentVariable("FunctionsHostExePath", hostPath);
                Environment.SetEnvironmentVariable("FunctionsHostExeSha256", ComputeHash(hostPath));
                Environment.SetEnvironmentVariable("FunctionApplicationPath", null);

                var settings = FunctionHostSettings.Load("app");

                Assert.Equal(hostPath, settings.FunctionsHostExePath);
                Assert.Equal("app", settings.FunctionApplicationPath);
            }
            finally
            {
                Environment.SetEnvironmentVariable("FunctionsHostExePath", originalHostPath);
                Environment.SetEnvironmentVariable("FunctionsHostExeSha256", originalHash);
                Environment.SetEnvironmentVariable("FunctionApplicationPath", originalAppPath);
                File.Delete(hostPath);
            }
        }

        [Fact]
        public void Load_expands_host_path_and_preserves_function_app_override()
        {
            var originalHostPath = Environment.GetEnvironmentVariable("FunctionsHostExePath");
            var originalHash = Environment.GetEnvironmentVariable("FunctionsHostExeSha256");
            var originalAppPath = Environment.GetEnvironmentVariable("FunctionApplicationPath");
            var originalDirectory = Environment.GetEnvironmentVariable("FUNCTIONS_HOST_TEST_DIRECTORY");
            var hostPath = Path.GetTempFileName();
            try
            {
                Environment.SetEnvironmentVariable("FUNCTIONS_HOST_TEST_DIRECTORY", Path.GetDirectoryName(hostPath));
                Environment.SetEnvironmentVariable("FunctionsHostExePath",
                    $"%FUNCTIONS_HOST_TEST_DIRECTORY%{Path.DirectorySeparatorChar}{Path.GetFileName(hostPath)}");
                Environment.SetEnvironmentVariable("FunctionsHostExeSha256", ComputeHash(hostPath));
                Environment.SetEnvironmentVariable("FunctionApplicationPath", "overridden-app");

                var settings = FunctionHostSettings.Load("default-app");

                Assert.Equal(hostPath, settings.FunctionsHostExePath);
                Assert.Equal("overridden-app", settings.FunctionApplicationPath);
            }
            finally
            {
                Environment.SetEnvironmentVariable("FunctionsHostExePath", originalHostPath);
                Environment.SetEnvironmentVariable("FunctionsHostExeSha256", originalHash);
                Environment.SetEnvironmentVariable("FunctionApplicationPath", originalAppPath);
                Environment.SetEnvironmentVariable("FUNCTIONS_HOST_TEST_DIRECTORY", originalDirectory);
                File.Delete(hostPath);
            }
        }

        [Fact]
        public void Load_does_not_reuse_an_earlier_host_path()
        {
            var originalHostPath = Environment.GetEnvironmentVariable("FunctionsHostExePath");
            var originalHash = Environment.GetEnvironmentVariable("FunctionsHostExeSha256");
            var hostPath = Path.GetTempFileName();
            try
            {
                Environment.SetEnvironmentVariable("FunctionsHostExePath", hostPath);
                Environment.SetEnvironmentVariable("FunctionsHostExeSha256", ComputeHash(hostPath));
                Assert.Equal(hostPath, FunctionHostSettings.Load("app").FunctionsHostExePath);

                Environment.SetEnvironmentVariable("FunctionsHostExePath", null);
                Assert.Throws<InvalidOperationException>(() => FunctionHostSettings.Load("app"));
            }
            finally
            {
                Environment.SetEnvironmentVariable("FunctionsHostExePath", originalHostPath);
                Environment.SetEnvironmentVariable("FunctionsHostExeSha256", originalHash);
                File.Delete(hostPath);
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("not-a-digest")]
        [InlineData("zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz")]
        public void Load_requires_a_valid_approved_digest(string? digest)
        {
            var originalHostPath = Environment.GetEnvironmentVariable("FunctionsHostExePath");
            var originalHash = Environment.GetEnvironmentVariable("FunctionsHostExeSha256");
            var hostPath = Path.GetTempFileName();
            try
            {
                Environment.SetEnvironmentVariable("FunctionsHostExePath", hostPath);
                Environment.SetEnvironmentVariable("FunctionsHostExeSha256", digest);

                var exception = Assert.Throws<InvalidOperationException>(() => FunctionHostSettings.Load("app"));

                Assert.Contains("FunctionsHostExeSha256", exception.Message);
            }
            finally
            {
                Environment.SetEnvironmentVariable("FunctionsHostExePath", originalHostPath);
                Environment.SetEnvironmentVariable("FunctionsHostExeSha256", originalHash);
                File.Delete(hostPath);
            }
        }

        [Fact]
        public void Load_rejects_a_host_that_does_not_match_the_approved_digest()
        {
            var originalHostPath = Environment.GetEnvironmentVariable("FunctionsHostExePath");
            var originalHash = Environment.GetEnvironmentVariable("FunctionsHostExeSha256");
            var hostPath = Path.GetTempFileName();
            try
            {
                Environment.SetEnvironmentVariable("FunctionsHostExePath", hostPath);
                Environment.SetEnvironmentVariable("FunctionsHostExeSha256", new string('0', 64));

                Assert.Throws<InvalidOperationException>(() => FunctionHostSettings.Load("app"));
            }
            finally
            {
                Environment.SetEnvironmentVariable("FunctionsHostExePath", originalHostPath);
                Environment.SetEnvironmentVariable("FunctionsHostExeSha256", originalHash);
                File.Delete(hostPath);
            }
        }

        [Fact]
        public void VerifyHostExecutable_rejects_a_host_changed_after_loading()
        {
            var originalHostPath = Environment.GetEnvironmentVariable("FunctionsHostExePath");
            var originalHash = Environment.GetEnvironmentVariable("FunctionsHostExeSha256");
            var hostPath = Path.GetTempFileName();
            try
            {
                Environment.SetEnvironmentVariable("FunctionsHostExePath", hostPath);
                Environment.SetEnvironmentVariable("FunctionsHostExeSha256", ComputeHash(hostPath));
                var settings = FunctionHostSettings.Load("app");

                File.WriteAllText(hostPath, "replaced");
                Environment.SetEnvironmentVariable("FunctionsHostExeSha256", ComputeHash(hostPath));

                Assert.Throws<InvalidOperationException>(() => settings.VerifyHostExecutable());
            }
            finally
            {
                Environment.SetEnvironmentVariable("FunctionsHostExePath", originalHostPath);
                Environment.SetEnvironmentVariable("FunctionsHostExeSha256", originalHash);
                File.Delete(hostPath);
            }
        }

        private static string ComputeHash(string path) =>
            Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
    }
}
