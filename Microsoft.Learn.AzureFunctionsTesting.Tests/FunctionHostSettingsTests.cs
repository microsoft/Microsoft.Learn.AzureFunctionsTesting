using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Microsoft.Learn.AzureFunctionsTesting.Tests
{
    public class FunctionHostSettingsTests : IDisposable
    {
        private readonly string testDirectory = Path.Combine(Path.GetTempPath(), "FunctionHostSettingsTests-" + Guid.NewGuid().ToString("N"));
        private readonly Dictionary<string, string?> originalEnvironment = new();

        public FunctionHostSettingsTests()
        {
            foreach (var name in new[] { "FunctionsHostExePath", "FunctionApplicationPath", "LOCALAPPDATA", "APPDATA", "SYSTEM_DEFAULTWORKINGDIRECTORY" })
            {
                originalEnvironment[name] = Environment.GetEnvironmentVariable(name);
                Environment.SetEnvironmentVariable(name, null);
            }

            Directory.CreateDirectory(testDirectory);
            Environment.SetEnvironmentVariable("LOCALAPPDATA", Path.Combine(testDirectory, "local"));
            Environment.SetEnvironmentVariable("APPDATA", Path.Combine(testDirectory, "roaming"));
            Environment.SetEnvironmentVariable("SYSTEM_DEFAULTWORKINGDIRECTORY", Path.Combine(testDirectory, "workspace"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public void Load_explains_setup_when_no_host_is_found(string? configuredPath)
        {
            Environment.SetEnvironmentVariable("FunctionsHostExePath", configuredPath);

            var exception = Assert.Throws<InvalidOperationException>(() => FunctionHostSettings.Load("app"));

            Assert.Contains("FunctionsHostExePath", exception.Message);
        }

        [WindowsTheory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public void Load_discovers_per_user_npm_when_no_path_is_configured(string? configuredPath)
        {
            var hostPath = CreateNpmHost();
            Environment.SetEnvironmentVariable("FunctionsHostExePath", configuredPath);

            var settings = FunctionHostSettings.Load("app");

            Assert.Equal(hostPath, settings.FunctionsHostExePath);
        }

        [WindowsTheory]
        [InlineData("cli")]
        [InlineData("cli_x64")]
        public void Load_prefers_visual_studio_over_per_user_npm(string cliDirectory)
        {
            var hostPath = CreateVisualStudioHost("4.15.0", cliDirectory);
            CreateNpmHost();

            var settings = FunctionHostSettings.Load("app");

            Assert.Equal(hostPath, settings.FunctionsHostExePath);
        }

        [WindowsTheory]
        [InlineData(false)]
        [InlineData(true)]
        public void Load_selects_the_latest_compatible_visual_studio_release(bool isolated)
        {
            var isolatedHost = CreateVisualStudioHost("4.15.0");
            var inProcessHost = CreateVisualStudioHost("4.15.0-inprocess");
            var otherMajorVersionHost = CreateVisualStudioHost("40.0.0");
            Directory.SetLastWriteTimeUtc(Path.GetDirectoryName(Path.GetDirectoryName(isolatedHost))!, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            Directory.SetLastWriteTimeUtc(Path.GetDirectoryName(Path.GetDirectoryName(inProcessHost))!, new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc));
            Directory.SetLastWriteTimeUtc(Path.GetDirectoryName(Path.GetDirectoryName(otherMajorVersionHost))!, new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc));
            var appPath = Path.Combine(testDirectory, "app");
            Directory.CreateDirectory(appPath);
            if (isolated)
            {
                File.WriteAllText(Path.Combine(appPath, "extensions.json"), "{}");
            }
            Environment.SetEnvironmentVariable("FunctionApplicationPath", appPath);

            var settings = FunctionHostSettings.Load("unused-app");

            Assert.Equal(isolated ? isolatedHost : inProcessHost, settings.FunctionsHostExePath);
            Assert.Equal(appPath, settings.FunctionApplicationPath);
        }

        [WindowsFact]
        public void Load_falls_back_to_per_user_npm_when_visual_studio_has_no_executable()
        {
            Directory.CreateDirectory(Path.Combine(testDirectory, "local", "AzureFunctionsTools", "Releases", "4.15.0"));
            var hostPath = CreateNpmHost();

            Assert.Equal(hostPath, FunctionHostSettings.Load("app").FunctionsHostExePath);
        }

        [Fact]
        public void Load_does_not_discover_a_workspace_installation()
        {
            CreateHost("workspace", "node_modules", "azure-functions-core-tools_4", "bin", "func.exe");

            Assert.Throws<InvalidOperationException>(() => FunctionHostSettings.Load("app"));
        }

        [Fact]
        public void Load_does_not_use_relative_per_user_directories()
        {
            Environment.SetEnvironmentVariable("LOCALAPPDATA", "local");
            Environment.SetEnvironmentVariable("APPDATA", "roaming");

            Assert.Throws<InvalidOperationException>(() => FunctionHostSettings.Load("app"));
        }

        [Fact]
        public void Load_rejects_a_relative_explicit_path_without_discovery()
        {
            CreateNpmHost();
            Environment.SetEnvironmentVariable("FunctionsHostExePath", "node_modules/azure-functions-core-tools/bin/func.exe");

            Assert.Throws<InvalidOperationException>(() => FunctionHostSettings.Load("app"));
        }

        [Fact]
        public void Load_rejects_a_missing_explicit_file_without_discovery()
        {
            CreateNpmHost();
            Environment.SetEnvironmentVariable("FunctionsHostExePath", Path.Combine(testDirectory, "missing", "func.exe"));

            Assert.Throws<FileNotFoundException>(() => FunctionHostSettings.Load("app"));
        }

        [Fact]
        public void Load_uses_the_explicit_path_instead_of_discovery()
        {
            var hostPath = CreateHost("explicit installation", "func.exe");
            CreateVisualStudioHost("4.15.0");
            CreateNpmHost();
            Environment.SetEnvironmentVariable("FunctionsHostExePath", hostPath);

            var settings = FunctionHostSettings.Load("app");

            Assert.Equal(hostPath, settings.FunctionsHostExePath);
            Assert.Equal("app", settings.FunctionApplicationPath);
        }

        [Fact]
        public void Load_expands_the_explicit_path_and_preserves_the_function_app_override()
        {
            var hostPath = CreateHost("local", "custom installation", "func.exe");
            Environment.SetEnvironmentVariable("FunctionsHostExePath",
                Path.Combine("%LOCALAPPDATA%", "custom installation", "func.exe"));
            Environment.SetEnvironmentVariable("FunctionApplicationPath", "overridden-app");

            var settings = FunctionHostSettings.Load("default-app");

            Assert.Equal(hostPath, settings.FunctionsHostExePath);
            Assert.Equal("overridden-app", settings.FunctionApplicationPath);
        }

        [WindowsFact]
        public void Load_does_not_reuse_an_earlier_explicit_path()
        {
            var configuredHost = CreateHost("explicit", "func.exe");
            var discoveredHost = CreateNpmHost();
            Environment.SetEnvironmentVariable("FunctionsHostExePath", configuredHost);
            Assert.Equal(configuredHost, FunctionHostSettings.Load("app").FunctionsHostExePath);

            Environment.SetEnvironmentVariable("FunctionsHostExePath", null);

            Assert.Equal(discoveredHost, FunctionHostSettings.Load("app").FunctionsHostExePath);
        }

        private string CreateVisualStudioHost(string release, string cliDirectory = "cli")
        {
            return CreateHost("local", "AzureFunctionsTools", "Releases", release, cliDirectory, "func.exe");
        }

        private string CreateNpmHost()
        {
            return CreateHost("roaming", "npm", "node_modules", "azure-functions-core-tools", "bin", "func.exe");
        }

        private string CreateHost(params string[] relativePath)
        {
            var path = Path.Combine(testDirectory, Path.Combine(relativePath));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, string.Empty);
            return path;
        }

        public void Dispose()
        {
            foreach (var variable in originalEnvironment)
            {
                Environment.SetEnvironmentVariable(variable.Key, variable.Value);
            }
            Directory.Delete(testDirectory, recursive: true);
        }

        private sealed class WindowsFactAttribute : FactAttribute
        {
            public WindowsFactAttribute()
            {
                if (!OperatingSystem.IsWindows())
                {
                    Skip = "Per-user Core Tools discovery is supported only on Windows.";
                }
            }
        }

        private sealed class WindowsTheoryAttribute : TheoryAttribute
        {
            public WindowsTheoryAttribute()
            {
                if (!OperatingSystem.IsWindows())
                {
                    Skip = "Per-user Core Tools discovery is supported only on Windows.";
                }
            }
        }
    }
}
