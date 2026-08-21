using Microsoft.Learn.AzureFunctionsTesting.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Learn.AzureFunctionsTesting
{
    public class FunctionFixture<T> : IFunctionFixture, IAsyncLifetime where T : IFunctionTestStartup
    {
        const int PortReleaseTimeoutSeconds = 10;

        static readonly JsonSerializerOptions EventGridJsonSerializerOptions = new()
        {
            Converters = { new CustomDateTimeConverter("yyyy-MM-ddTHH:mm:ssZ") }
        };

        readonly FunctionTestConfigurationBuilder builder;
        Process? hostProcess;

        public FunctionFixture()
        {
            builder = CreateBuilder();
            var fts = Activator.CreateInstance<T>();
            fts.Configure(builder);

            Client = new HttpClient
            {
                BaseAddress = new Uri($"http://localhost:{builder.Port}")
#if DEBUG
    ,
                Timeout = TimeSpan.FromMinutes(5)
#endif
            };
        }

        protected virtual FunctionTestConfigurationBuilder CreateBuilder()
        {
            return new FunctionTestConfigurationBuilder();
        }

        public virtual async Task InitializeAsync()
        {
            var settings = FunctionHostSettings.Load(builder.FunctionAppPath!);
            var functionAppPath = settings.FunctionApplicationPath;
            if (string.IsNullOrWhiteSpace(functionAppPath))
            {
                throw new Exception("FunctionApplicationPath not set. It should be set to a relative path similar to: ..\\..\\..\\..\\..\\Your.Function.App\\bin\\Debug\\net8.0");
            }

            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), functionAppPath);
            if (!Directory.Exists(fullPath))
            {
                throw new Exception($"Invalid FunctionApplicationPath - '{fullPath}' does not exist.");
            }

            var functionsHostExePath = Environment.ExpandEnvironmentVariables(settings.FunctionsHostExePath!);
            if (!File.Exists(functionsHostExePath))
            {
                throw new Exception($"Invalid Functions Host Exe Path - '{functionsHostExePath}' does not exist.");
            }

            Console.WriteLine($"FunctionsHostExePath: {settings.FunctionsHostExePath}");
            Console.WriteLine($"FunctionApplicationPath: {settings.FunctionApplicationPath}");
            Console.WriteLine($"CurrentDirectory: {Directory.GetCurrentDirectory()}");

            var envVars = new Dictionary<string, string>();
            foreach (var plugin in builder.GetPlugins())
            {
                await plugin.InitializeAsync(envVars);
            }
            foreach (var action in builder.ConfigureEnvironmentVariablesActions)
            {
                action?.Invoke(envVars);
            }

            envVars["IS_FUNCTIONS_TEST"] = "true";

            // Deliberate: tests may only run one function app at a time. Killing any stray
            // host first stops the tests from silently calling an unrelated app left running
            // on the same port. This means concurrent runs on one machine are not supported,
            // so anything running these tests must not overlap them, e.g. CI must serialize
            // the per-framework passes of a multi-targeted project.
            var currentFuncProcesses = Process.GetProcessesByName("func");
            foreach (Process process in currentFuncProcesses)
            {
                process.Kill(true);
                process.WaitForExit();
            }

            // Killing the stale hosts above only covers func. If anything else is still
            // listening on the port, the readiness check below would happily talk to it and
            // the tests would silently run against the wrong application, so fail loudly
            // instead. A just-killed host can take a moment to release the port, so allow
            // a short grace period before giving up.
            var portFreeBy = DateTime.UtcNow.AddSeconds(PortReleaseTimeoutSeconds);
            while (IsPortInUse(builder.Port) && DateTime.UtcNow < portFreeBy)
            {
                await Task.Delay(200);
            }

            if (IsPortInUse(builder.Port))
            {
                throw new Exception(
                    $"Port {builder.Port} is already in use by another process, so the Functions Host Runtime " +
                    "could not be started there. The tests would otherwise run against whatever is already " +
                    "listening on that port. Stop that process, or call SetFunctionAppPort() to use a different port.");
            }

            hostProcess = new Process
            {
                StartInfo =
                {
                    FileName = functionsHostExePath,
                    // --address requires Azure Functions Core Tools 4.14.0 or later, which changes the
                    // default bind address from 0.0.0.0 to the IPv4 loopback address. Earlier versions
                    // ignore the flag, so passing it is safe and makes the address configurable once 4.14.0 ships.
                    Arguments = $"start -p {builder.Port} --address {builder.Address} {(builder.EnableAuth ? "--enableAuth" : null)}",
                    WorkingDirectory = functionAppPath
                }
            };

            foreach (var action in builder.BeforeProcessStartActions)
            {
                await (action?.Invoke(envVars) ?? Task.CompletedTask);
            }

            foreach (var kvp in envVars)
            {
                hostProcess.StartInfo.EnvironmentVariables[kvp.Key] = kvp.Value;
            }

            var success = hostProcess.Start();
            if (!success)
            {
                throw new InvalidOperationException("Could not start Azure Functions host.");
            }

            foreach (var action in builder.AfterProcessStartActions)
            {
                await (action?.Invoke(hostProcess.Id) ?? Task.CompletedTask);
            }

            var maxSeconds = builder.StartupTimeout;
            var ready = false;
            var waitTil = DateTime.UtcNow.AddSeconds(maxSeconds);
            while (!ready && DateTime.UtcNow < waitTil)
            {
                // If the host process dies, polling until the timeout expires just hides the real failure
                if (hostProcess.HasExited)
                {
                    throw new Exception($"The Functions Host Runtime process ('{functionsHostExePath}') exited unexpectedly with exit code {hostProcess.ExitCode}.");
                }

                try
                {
                    var response = await Client.GetAsync("");
                    ready = ((int)response.StatusCode) < 500;
                }
                catch
                {
                    ready = false;
                }
                if (!ready)
                {
                    await Task.Delay(1000);
                }
            }

            if (!ready)
            {
                throw new Exception($"The Functions Host Runtime did not start properly after {maxSeconds} seconds.");
            }
        }

        public virtual async Task DisposeAsync()
        {
            if (hostProcess != null)
            {
                if (!hostProcess.HasExited)
                {
                    hostProcess.Kill();
                }
                hostProcess.Dispose();
            }

            foreach (var plugin in builder.GetPlugins())
            {
                try
                {
                    await plugin.DisposeAsync();
                }
                catch
                {
                }
            }
        }

        static bool IsPortInUse(int port)
        {
            // Only sockets in the LISTEN state are returned, so a port left in TIME_WAIT by a
            // previous run is correctly treated as free.
            foreach (var listener in IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners())
            {
                if (listener.Port == port)
                {
                    return true;
                }
            }

            return false;
        }

        public TPlugin? GetPlugin<TPlugin>(string name) where TPlugin : IFunctionTestPlugin
        {
            return builder.GetPlugin<TPlugin>(name);
        }

        public HttpClient Client { get; private set; }

        public async Task TriggerNonHttpFunctionAsync(string functionName, object? payload = null)
        {
            var json = payload != null ? JsonSerializer.Serialize(payload) : null;
            var request = new HttpRequestMessage(HttpMethod.Post, $"admin/functions/{functionName}")
            {
                Content = new StringContent(JsonSerializer.Serialize(new { input = json }), Encoding.UTF8, "application/json")
            };
            var response = await Client.SendAsync(request);
            if (response.StatusCode != HttpStatusCode.Accepted)
            {
                throw new InvalidOperationException($"Non-HTTP function '{functionName}' failed");
            }
        }

        public async Task<HttpResponseMessage> SendEventGridMessageAsync(string eventType, object payload, string triggerFunctionName)
        {
            object requestBody = new
            {
                id = "any/id",
                topic = "any/topic",
                subject = "any/subject",
                data = payload,
                eventType = eventType,
                dataVersion = "1.0",
                metadataVersion = "1",
                eventTime = DateTime.UtcNow
            };

            var message = new HttpRequestMessage(HttpMethod.Post, $"runtime/webhooks/EventGrid?functionName={triggerFunctionName}")
            {
                Content = new StringContent(JsonSerializer.Serialize(requestBody, EventGridJsonSerializerOptions), encoding: null, "application/json")
            };
            message.Headers.TryAddWithoutValidation("aeg-event-type", "Notification");

            return await Client.SendAsync(message);
        }
    }
}