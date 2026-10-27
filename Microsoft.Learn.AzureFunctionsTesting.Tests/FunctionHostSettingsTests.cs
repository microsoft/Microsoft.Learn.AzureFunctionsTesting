using Microsoft.Learn.AzureFunctionsTesting;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Microsoft.Learn.AzureFunctionsTesting.Tests
{
    public class FunctionHostSettingsTests : IDisposable
    {
        private readonly string root = Path.Combine(Path.GetTempPath(), $"FunctionHostSettingsTests-{Guid.NewGuid():N}");

        [Fact]
        public void Explicit_path_takes_precedence_over_discovery()
        {
            var configuredPath = CreateFile("configured", "func.exe");
            var discoveredPath = CreateFile("discovered", "func.exe");

            Assert.Equal(configuredPath, FunctionHostSettings.ResolveHostExecutablePath(configuredPath, new[] { discoveredPath }));
        }

        [Fact]
        public void Explicit_path_expands_environment_variables()
        {
            var configuredPath = CreateFile("configured", "func.exe");
            var variable = $"FUNCTION_HOST_TEST_{Guid.NewGuid():N}";
            Environment.SetEnvironmentVariable(variable, Path.GetDirectoryName(configuredPath));
            try
            {
                var path = Path.Combine($"%{variable}%", "func.exe");

                Assert.Equal(configuredPath, FunctionHostSettings.ResolveHostExecutablePath(path, Array.Empty<string>()));
            }
            finally
            {
                Environment.SetEnvironmentVariable(variable, null);
            }
        }

        [Theory]
        [InlineData("func.exe")]
        [InlineData(".\\func.exe")]
        [InlineData("C:func.exe")]
        [InlineData("\\func.exe")]
        [InlineData(" ")]
        public void Non_absolute_override_fails_without_fallback(string configuredPath)
        {
            var discoveredPath = CreateFile("discovered", "func.exe");

            var error = Assert.Throws<InvalidOperationException>(() =>
                FunctionHostSettings.ResolveHostExecutablePath(configuredPath, new[] { discoveredPath }));

            Assert.Contains("fully qualified absolute path", error.Message);
        }

        [Fact]
        public void Missing_explicit_file_fails_without_fallback()
        {
            var discoveredPath = CreateFile("discovered", "func.exe");

            var error = Assert.Throws<InvalidOperationException>(() =>
                FunctionHostSettings.ResolveHostExecutablePath(Path.Combine(root, "missing.exe"), new[] { discoveredPath }));

            Assert.Contains("does not exist", error.Message);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Discovery_selects_first_existing_absolute_candidate(string? configuredPath)
        {
            var first = CreateFile("first", "func.exe");
            var second = CreateFile("second", "func.exe");
            var candidates = new[] { Path.GetRelativePath(Directory.GetCurrentDirectory(), first), Path.Combine(root, "missing.exe"), first, second };

            Assert.Equal(first, FunctionHostSettings.ResolveHostExecutablePath(configuredPath, candidates));
        }

        [Fact]
        public void Missing_installation_has_actionable_error()
        {
            var error = Assert.Throws<InvalidOperationException>(() =>
                FunctionHostSettings.ResolveHostExecutablePath(null, new[] { Path.Combine(root, "missing.exe") }));

            Assert.Contains("Install Core Tools", error.Message);
            Assert.Contains("FunctionsHostExePath", error.Message);
        }

        [Fact]
        public void Windows_discovery_preserves_latest_release_and_cli_precedence()
        {
            var localData = Path.Combine(root, "local");
            var appData = Path.Combine(root, "roaming");
            var older = CreateFile("local", "AzureFunctionsTools", "Releases", "4.old", "cli", "func.exe");
            var cli = CreateFile("local", "AzureFunctionsTools", "Releases", "4.latest", "cli", "func.exe");
            var cliX64 = CreateFile("local", "AzureFunctionsTools", "Releases", "4.latest", "cli_x64", "func.exe");
            var npm = CreateFile("roaming", "npm", "node_modules", "azure-functions-core-tools", "bin", "func.exe");
            Directory.SetLastWriteTimeUtc(Path.Combine(localData, "AzureFunctionsTools", "Releases", "4.old"), DateTime.UtcNow.AddDays(-2));
            Directory.SetLastWriteTimeUtc(Path.Combine(localData, "AzureFunctionsTools", "Releases", "4.latest"), DateTime.UtcNow.AddDays(-1));

            var candidates = FunctionHostSettings.GetHostExecutableCandidates(root, localData, appData, true, false).ToArray();

            Assert.Equal(new[] { cli, cliX64, npm }, candidates);
            Assert.DoesNotContain(older, candidates);
            Assert.Equal(cli, FunctionHostSettings.ResolveHostExecutablePath(null, candidates));
            File.Delete(cli);
            Assert.Equal(cliX64, FunctionHostSettings.ResolveHostExecutablePath(null, candidates));
            File.Delete(cliX64);
            Assert.Equal(npm, FunctionHostSettings.ResolveHostExecutablePath(null, candidates));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Windows_discovery_preserves_runtime_classification(bool isolated)
        {
            var localData = Path.Combine(root, "local");
            var inProcess = CreateFile("local", "AzureFunctionsTools", "Releases", "4.latest-inprocess", "cli", "func.exe");
            var outOfProcess = CreateFile("local", "AzureFunctionsTools", "Releases", "4.isolated", "cli", "func.exe");
            CreateFile("local", "AzureFunctionsTools", "Releases", "3.newer", "cli", "func.exe");
            Directory.SetLastWriteTimeUtc(Path.Combine(localData, "AzureFunctionsTools", "Releases", "4.latest-inprocess"), DateTime.UtcNow.AddDays(-1));
            Directory.SetLastWriteTimeUtc(Path.Combine(localData, "AzureFunctionsTools", "Releases", "4.isolated"), DateTime.UtcNow.AddDays(-2));
            if (isolated)
            {
                CreateFile("extensions.json");
            }

            var candidates = FunctionHostSettings.GetHostExecutableCandidates(root, localData, "", true, false);

            Assert.Equal(isolated ? outOfProcess : inProcess, FunctionHostSettings.ResolveHostExecutablePath(null, candidates));
        }

        [Fact]
        public void Windows_discovery_skips_unavailable_profile_roots()
        {
            Assert.Empty(FunctionHostSettings.GetHostExecutableCandidates(root, "", "", true, false));
        }

        [Fact]
        public void Windows_discovery_without_visual_studio_only_probes_per_user_npm()
        {
            var appData = Path.Combine(root, "roaming");
            var expected = Path.Combine(appData, "npm", "node_modules", "azure-functions-core-tools", "bin", "func.exe");

            var candidates = FunctionHostSettings.GetHostExecutableCandidates(root, Path.Combine(root, "local"), appData, true, false);

            Assert.Equal(new[] { expected }, candidates);
        }

        [Fact]
        public void MacOS_discovery_only_probes_homebrew()
        {
            var candidates = FunctionHostSettings.GetHostExecutableCandidates(root, root, root, false, true);

            Assert.Equal(new[] { "/opt/homebrew/bin/func" }, candidates);
        }

        [Fact]
        public void Other_platforms_require_explicit_configuration()
        {
            Assert.Empty(FunctionHostSettings.GetHostExecutableCandidates(root, root, root, false, false));
        }

        private string CreateFile(params string[] parts)
        {
            var path = Path.Combine(new[] { root }.Concat(parts).ToArray());
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, "");
            return path;
        }

        public void Dispose()
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }
}
