using Microsoft.Learn.AzureFunctionsTesting.Extension.MockHttpServer;
using System.IO;
using System.Net;

namespace Tests.MyFunctionApp.Helpers
{
    internal class EmailServer : HttpServer
    {
        protected override void RequestHandler(HttpListenerRequest req, MockHttpResponse res)
        {
            using var streamReader = new StreamReader(req.InputStream);
            var data = streamReader.ReadToEnd();

            if (data.Contains("fail"))
            {
                res.StatusCode = 500;
            }
            else
            {
                res.StatusCode = 200;
            }
        }
    }
}
