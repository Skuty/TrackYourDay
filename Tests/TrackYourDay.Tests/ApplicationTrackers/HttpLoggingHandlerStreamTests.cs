// Tests/TrackYourDay.Tests/ApplicationTrackers/HttpLoggingHandlerStreamTests.cs
using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using TrackYourDay.Core.ApplicationTrackers;

namespace TrackYourDay.Tests.ApplicationTrackers;

public class HttpLoggingHandlerStreamTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly Mock<HttpMessageHandler> _innerHandlerMock;

    public HttpLoggingHandlerStreamTests()
    {
        _loggerMock = new Mock<ILogger>();
        _innerHandlerMock = new Mock<HttpMessageHandler>();
    }

    [Fact]
    public async Task GivenFailedRequest_WhenSendingAsync_ThenResponseContentIsStillReadable()
    {
        // Given
        var errorBody = "{\"error\": \"Invalid request\"}";
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(errorBody)
        };
        _innerHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var handler = new HttpLoggingHandler(_loggerMock.Object, "TestService")
        {
            InnerHandler = _innerHandlerMock.Object
        };

        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.test.com") };
        var request = new HttpRequestMessage(HttpMethod.Post, "/test");

        // When
        var httpResponse = await client.SendAsync(request);

        // Then - response content should still be readable by caller
        var content = await httpResponse.Content.ReadAsStringAsync();
        content.Should().Be(errorBody);
    }

    [Fact]
    public async Task GivenFailedRequestWithContentHeaders_WhenSendingAsync_ThenContentHeadersArePreserved()
    {
        // Given
        var errorBody = "{\"error\": \"Invalid request\"}";
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(errorBody, System.Text.Encoding.UTF8, "application/json")
        };
        _innerHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var handler = new HttpLoggingHandler(_loggerMock.Object, "TestService")
        {
            InnerHandler = _innerHandlerMock.Object
        };

        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.test.com") };
        var request = new HttpRequestMessage(HttpMethod.Post, "/test");

        // When
        var httpResponse = await client.SendAsync(request);

        // Then - content headers should be preserved
        httpResponse.Content.Headers.ContentType.Should().NotBeNull();
        httpResponse.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        httpResponse.Content.Headers.ContentType.CharSet.Should().Be("utf-8");
    }

    [Fact]
    public async Task GivenFailedRequestReadMultipleTimes_WhenSendingAsync_ThenContentIsReadableEachTime()
    {
        // Given
        var errorBody = "{\"error\": \"Invalid request\"}";
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(errorBody)
        };
        _innerHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var handler = new HttpLoggingHandler(_loggerMock.Object, "TestService")
        {
            InnerHandler = _innerHandlerMock.Object
        };

        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.test.com") };
        var request = new HttpRequestMessage(HttpMethod.Post, "/test");

        // When
        var httpResponse = await client.SendAsync(request);

        // Then - content should be readable multiple times
        var content1 = await httpResponse.Content.ReadAsStringAsync();
        var content2 = await httpResponse.Content.ReadAsStringAsync();
        
        content1.Should().Be(errorBody);
        content2.Should().Be(errorBody);
    }

    [Fact]
    public async Task GivenSuccessfulRequest_WhenSendingAsync_ThenResponseContentIsStillReadable()
    {
        // Given
        var responseBody = "{\"data\": \"success\"}";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseBody)
        };
        _innerHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var handler = new HttpLoggingHandler(_loggerMock.Object, "TestService")
        {
            InnerHandler = _innerHandlerMock.Object
        };

        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.test.com") };
        var request = new HttpRequestMessage(HttpMethod.Get, "/test");

        // When
        var httpResponse = await client.SendAsync(request);

        // Then - response content should still be readable by caller (not logged but should not be broken)
        var content = await httpResponse.Content.ReadAsStringAsync();
        content.Should().Be(responseBody);
    }
}
