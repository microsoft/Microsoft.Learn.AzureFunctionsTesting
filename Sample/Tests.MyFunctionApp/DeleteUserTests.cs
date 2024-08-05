using Microsoft.Learn.AzureFunctionsTesting;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Tests.MyFunctionApp.Helpers;
using Xunit;

namespace Tests.MyFunctionApp
{
    public class DeleteUserTests
    {
        private readonly FunctionFixture<TestStartup> fixture;

        public DeleteUserTests(FunctionFixture<TestStartup> fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public async Task DeleteUser_returns_OK_when_email_server_succeeds()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var name = Guid.NewGuid().ToString();
            await fixture.AddUserToDatabase(userId, name);

            // Act
            var request = new HttpRequestMessage(HttpMethod.Delete, $"api/user/{userId}");
            var response = await fixture.Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task DeleteUser_returns_500_when_email_server_fails()
        {
            // Arrange
            var userId = "fail" + Guid.NewGuid().ToString();
            var name = Guid.NewGuid().ToString();
            await fixture.AddUserToDatabase(userId, name);

            // Act
            var request = new HttpRequestMessage(HttpMethod.Delete, $"api/user/{userId}");
            var response = await fixture.Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }
    }
}