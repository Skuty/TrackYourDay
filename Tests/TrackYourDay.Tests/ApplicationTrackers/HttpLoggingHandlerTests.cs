// Tests/TrackYourDay.Tests/ApplicationTrackers/HttpLoggingHandlerTests.cs
using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using TrackYourDay.Core.ApplicationTrackers;

namespace TrackYourDay.Tests.ApplicationTrackers;

public class HttpLoggingHandlerTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly Mock<HttpMessageHandler> _innerHandlerMock;

    public HttpLoggingHandlerTests()
    {
        _loggerMock = new Mock<ILogger>();
        _innerHandlerMock = new Mock<HttpMessageHandler>();
    }

    [Fact]
    public async Task GivenSuccessfulRequest_WhenSendingAsync_ThenLogsDebugWithStatusAndDuration()
    {
        // Given
        var response = new HttpResponseMessage(HttpStatusCode.OK);
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
        await client.SendAsync(request);

        // Then
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("TestService") && 
                                                v.ToString()!.Contains("200") &&
                                                v.ToString()!.Contains("ms")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenFailedRequest_WhenSendingAsync_ThenLogsErrorWithStatusAndResponseBody()
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
        await client.SendAsync(request);

        // Then
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("TestService") && 
                                                v.ToString()!.Contains("400") &&
                                                v.ToString()!.Contains("ms") &&
                                                v.ToString()!.Contains(errorBody)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenExceptionDuringRequest_WhenSendingAsync_ThenLogsErrorWithExceptionMessage()
    {
        // Given
        var expectedException = new HttpRequestException("Connection failed");
        _innerHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(expectedException);

        var handler = new HttpLoggingHandler(_loggerMock.Object, "TestService")
        {
            InnerHandler = _innerHandlerMock.Object
        };

        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.test.com") };
        var request = new HttpRequestMessage(HttpMethod.Get, "/test");

        // When
        var act = async () => await client.SendAsync(request);

        // Then
        await act.Should().ThrowAsync<HttpRequestException>();
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("TestService") && 
                                                v.ToString()!.Contains("failed after") &&
                                                v.ToString()!.Contains("ms")),
                It.Is<Exception>(ex => ex == expectedException),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void GivenNullLogger_WhenCreatingHandler_ThenThrowsArgumentNullException()
    {
        // Given / When
        var act = () => new HttpLoggingHandler(null!, "TestService");

        // Then
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GivenNullServiceName_WhenCreatingHandler_ThenThrowsArgumentException()
    {
        // Given / When
        var act = () => new HttpLoggingHandler(_loggerMock.Object, null!);

        // Then
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GivenEmptyServiceName_WhenCreatingHandler_ThenThrowsArgumentException()
    {
        // Given / When
        var act = () => new HttpLoggingHandler(_loggerMock.Object, "");

        // Then
        act.Should().Throw<ArgumentException>();
    }
}
