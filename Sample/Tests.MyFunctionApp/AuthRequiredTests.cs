using Microsoft.Learn.AzureFunctionsTesting;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Tests.MyFunctionApp.Helpers;
using Xunit;

namespace Tests.MyFunctionApp
{
    public class AuthRequiredTests
    {
        private readonly FunctionFixture<TestStartup> fixture;

        public AuthRequiredTests(FunctionFixture<TestStartup> fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public async Task AuthRequired_returns_401_when_not_authenticated()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "api/auth-required");

            // Act
            var response = await fixture.Client.SendAsync(request);

            // Assert
            Assert.True(response.StatusCode == HttpStatusCode.Unauthorized,
                $"response.StatusCode is {response.StatusCode} but {HttpStatusCode.Unauthorized} was expected."
                + Environment.NewLine
                + "If this fails, ensure Azure Functions Core Tools is version 4.0.6821 or later. https://github.com/Azure/azure-functions-core-tools/releases/tag/4.0.6821");
        }
    }
}
