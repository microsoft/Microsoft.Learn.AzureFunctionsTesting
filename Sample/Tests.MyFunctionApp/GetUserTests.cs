using Microsoft.Learn.AzureFunctionsTesting;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Tests.MyFunctionApp.Helpers;
using Xunit;

namespace Tests.MyFunctionApp
{
    public class GetUserTests
    {
        private readonly FunctionFixture<TestStartup> fixture;

        public GetUserTests(FunctionFixture<TestStartup> fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public async Task GetUser_returns_user_when_userId_exists()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var name = Guid.NewGuid().ToString();
            await fixture.AddUserToDatabase(userId, name);

            // Act
            var request = new HttpRequestMessage(HttpMethod.Get, $"api/user/{userId}");
            var response = await fixture.Client.SendAsync(request);
            var user = await response.Content.ReadFromJsonAsync<User>();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(user);
            Assert.Equal(userId, user.Id);
            Assert.Equal(name, user.Name);
        }

        [Fact]
        public async Task GetUser_returns_404_when_userId_doesnt_exist()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();

            // Act
            var request = new HttpRequestMessage(HttpMethod.Get, $"api/user/{userId}");
            var response = await fixture.Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}