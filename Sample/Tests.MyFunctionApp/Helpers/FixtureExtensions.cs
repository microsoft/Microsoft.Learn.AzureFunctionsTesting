using Microsoft.Azure.Cosmos;
using Microsoft.Learn.AzureFunctionsTesting;
using Microsoft.Learn.AzureFunctionsTesting.Extension.MockCosmos;
using System.Threading.Tasks;

namespace Tests.MyFunctionApp.Helpers
{
    public static class FixtureExtensions
    {
        const string databaseName = "MyDb";
        const string containerName = "Users";

        public static async Task AddUserToDatabase<T>(this FunctionFixture<T> fixture, string userId, string name) where T : IFunctionTestStartup
        {
            var cosmos = fixture.GetCosmos();
            if (cosmos == null) return;
            var container = cosmos.GetContainer(databaseName, containerName);
            await container.UpsertItemAsync(new User { Id = userId, Name = name }, new PartitionKey(userId));
        }
    }
}
