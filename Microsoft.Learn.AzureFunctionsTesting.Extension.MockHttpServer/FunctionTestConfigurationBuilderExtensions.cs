using Microsoft.Learn.AzureFunctionsTesting.Core;
using System;
using System.Net;

namespace Microsoft.Learn.AzureFunctionsTesting.Extension.MockHttpServer
{
    public static class FunctionTestConfigurationBuilderExtensions
    {
        public static GenericHttpServer UseMockServer(this IFunctionTestConfigurationBuilder builder, string name, Action<HttpListenerRequest, MockHttpResponse> requestHandler)
        {
            var server = new GenericHttpServer(requestHandler);
            return builder.UseMockServer(name, server);
        }

        public static T UseMockServer<T>(this IFunctionTestConfigurationBuilder builder, string name) where T : HttpServer
        {
            var server = Activator.CreateInstance<T>();
            return builder.UseMockServer(name, server);
        }

        public static T UseMockServer<T>(this IFunctionTestConfigurationBuilder builder, string name, T server) where T : HttpServer
        {
            var plugin = new MockHttpServerPlugin(server);
            builder.RegisterPlugin(plugin, name);
            return (T)plugin.Server;
        }
    }
}
