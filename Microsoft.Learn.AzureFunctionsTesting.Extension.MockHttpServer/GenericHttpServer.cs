using System;
using System.Net;

namespace Microsoft.Learn.AzureFunctionsTesting.Extension.MockHttpServer
{
    public class GenericHttpServer : HttpServer
    {
        private readonly Action<HttpListenerRequest, MockHttpResponse> requestHandlerDelegate;

        public GenericHttpServer(Action<HttpListenerRequest, MockHttpResponse> requestHandlerDelegate)
        {
            this.requestHandlerDelegate = requestHandlerDelegate;
        }

        protected override void RequestHandler(HttpListenerRequest req, MockHttpResponse res)
        {
            requestHandlerDelegate(req, res);
        }
    }
}
