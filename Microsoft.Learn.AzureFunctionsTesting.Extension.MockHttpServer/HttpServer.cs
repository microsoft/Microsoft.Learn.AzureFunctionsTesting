using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Microsoft.Learn.AzureFunctionsTesting.Extension.MockHttpServer
{
    public abstract class HttpServer : IDisposable
    {
        static int nextPort = 40000;
        readonly HttpListener listener;
        readonly int port;

        public HttpServer()
        {
            this.listener = new HttpListener();
            this.port = nextPort++;
            listener.Prefixes.Add($"http://localhost:{port}/");
        }

        public string Url => $"http://localhost:{port}/";

        public void Start()
        {
            listener.Start();
            Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        var context = listener.GetContext();

                        try
                        {
                            var url = context.Request.RawUrl;
                            Console.WriteLine($"MockHttpServer received request: {url}");
                            var response = new MockHttpResponse();
                            RequestHandler(context.Request, response);
                            context.Response.StatusCode = response.StatusCode;
                            foreach (var header in response.Headers)
                            {
                                foreach (var val in header.Value)
                                {
                                    if (val != null) context.Response.AppendHeader(header.Key, val);
                                }
                            }

                            if (context.Response.Headers[HeaderNames.ContentType] == null)
                            {
                                context.Response.Headers.Add(HeaderNames.ContentType, "application/json");
                            }

                            if (response.Body != null)
                            {
                                context.Response.Close(response.Body, true);
                            }
                            else
                            {
                                context.Response.Close();
                            }
                        }
                        catch (Exception ex)
                        {
                            context.Response.Headers.Clear();
                            context.Response.StatusCode = 500;
                            context.Response.Close(Encoding.UTF8.GetBytes(ex.Message), true);
                        }
                    }
                    catch
                    {
                        // handle shutdown
                        return;
                    }
                }
            });
        }

        public void Stop()
        {
            listener.Stop();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            if (listener.IsListening) listener.Stop();
            ((IDisposable)listener).Dispose();
        }

        protected abstract void RequestHandler(HttpListenerRequest req, MockHttpResponse res);
    }

    public class MockHttpResponse
    {
        public int StatusCode { get; set; } = 404;
        public IDictionary<string, StringValues> Headers { get; } = new Dictionary<string, StringValues>();
        public byte[]? Body { get; set; }

        public void FromJson(object obj, int statusCode = 200)
        {
            StatusCode = statusCode;
            Headers["Content-Type"] = "application/json";
            var json = JsonSerializer.Serialize(obj);
            Body = Encoding.UTF8.GetBytes(json);
        }
    }
}
