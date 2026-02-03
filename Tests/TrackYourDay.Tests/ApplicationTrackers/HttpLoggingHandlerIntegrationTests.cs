// Tests/TrackYourDay.Tests/ApplicationTrackers/HttpLoggingHandlerIntegrationTests.cs
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using TrackYourDay.Core.ApplicationTrackers;

namespace TrackYourDay.Tests.ApplicationTrackers;

public class HttpLoggingHandlerIntegrationTests
{
    [Fact]
    public async Task GivenHttpClientFactory_WhenCreatingClientWithLoggingHandler_ThenHandlerIsInPipeline()
    {
        // Given
        var services = new ServiceCollection();
        var loggerMock = new Mock<ILogger<HttpLoggingHandler>>();
        
        services.AddSingleton(loggerMock.Object);
        services.AddHttpClient("TestClient")
            .AddHttpMessageHandler(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<HttpLoggingHandler>>();
                return new HttpLoggingHandler(logger, "TestService");
            })
            .ConfigurePrimaryHttpMessageHandler(() => new TestHttpMessageHandler(HttpStatusCode.OK));

        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();

        // When
        var client = factory.CreateClient("TestClient");
        var response = await client.GetAsync("https://test.com/api");

        // Then
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("TestService") && 
                                                v.ToString()!.Contains("200")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;

        public TestHttpMessageHandler(HttpStatusCode statusCode)
        {
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(_statusCode));
        }
    }
}
