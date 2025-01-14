using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace MyFunctionApp
{
    public class MyFunctions
    {
        const string databaseName = "MyDb";
        const string userContainerName = "Users";

        private readonly ILogger logger;
        private readonly CosmosClient cosmos;
        private readonly IHttpClientFactory httpClientFactory;

        public MyFunctions(ILoggerFactory loggerFactory, CosmosClient cosmos, IHttpClientFactory httpClientFactory)
        {
            logger = loggerFactory.CreateLogger<MyFunctions>();
            this.cosmos = cosmos;
            this.httpClientFactory = httpClientFactory;
        }

        [Function("GetUser")]
        public async Task<HttpResponseData> GetUser([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "user/{userId}")] HttpRequestData req, string userId)
        {
            logger.LogInformation("GetUser called");

            var response = req.CreateResponse();

            var container = cosmos.GetDatabase(databaseName).GetContainer(userContainerName);
            try
            {
                var result = await container.ReadItemAsync<User>(userId, new PartitionKey(userId));
                var user = result.Resource;
                await response.WriteAsJsonAsync(user);
            }
            catch
            {
                response.StatusCode = HttpStatusCode.NotFound;
            }

            return response;
        }


        [Function("DeleteUser")]
        public async Task<HttpResponseData> DeleteUser([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "user/{userId}")] HttpRequestData req, string userId)
        {
            logger.LogInformation("DeleteUser called");

            var container = cosmos.GetDatabase(databaseName).GetContainer(userContainerName);
            _ = await container.DeleteItemAsync<User>(userId, new PartitionKey(userId));

            var emailServer = httpClientFactory.CreateClient("EmailService");
            var emailResponse = await emailServer.PostAsync("/remove", JsonContent.Create(userId));
            emailResponse.EnsureSuccessStatusCode();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(true);
            return response;
        }

        [Function("AuthRequired")]
        public HttpResponseData AuthRequired([HttpTrigger(AuthorizationLevel.Function, "get", Route = "auth-required")] HttpRequestData req)
        {
            logger.LogInformation("AuthRequired called");

            var response = req.CreateResponse(HttpStatusCode.OK);

            return response;
        }
    }
}
