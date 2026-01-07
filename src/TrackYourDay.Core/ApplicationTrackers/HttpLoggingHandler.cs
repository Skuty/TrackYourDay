// src/TrackYourDay.Core/ApplicationTrackers/HttpLoggingHandler.cs
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace TrackYourDay.Core.ApplicationTrackers;

/// <summary>
/// HTTP message handler that logs request details including duration, status, and errors.
/// </summary>
public class HttpLoggingHandler : DelegatingHandler
{
    private readonly ILogger _logger;
    private readonly string _serviceName;

    public HttpLoggingHandler(ILogger logger, string serviceName)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        _logger = logger;
        _serviceName = serviceName;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        HttpResponseMessage? response = null;
        Exception? exception = null;

        try
        {
            response = await base.SendAsync(request, cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            exception = ex;
            throw;
        }
        finally
        {
            stopwatch.Stop();
            LogHttpRequest(request, response, stopwatch.Elapsed, exception);
        }
    }

    private void LogHttpRequest(
        HttpRequestMessage request,
        HttpResponseMessage? response,
        TimeSpan duration,
        Exception? exception)
    {
        var method = request.Method.Method;
        var uri = request.RequestUri?.ToString() ?? "unknown";
        var statusCode = response?.StatusCode;
        var statusCodeInt = (int?)statusCode;
        var durationMs = duration.TotalMilliseconds;

        if (exception != null)
        {
            _logger.LogInformation(
                "{ServiceName} HTTP {Method} {Uri} failed after {DurationMs}ms with exception: {ExceptionMessage}",
                _serviceName,
                method,
                uri,
                durationMs,
                exception.Message);
        }
        else
        {
            _logger.LogInformation(
                "{ServiceName} HTTP {Method} {Uri} completed with status {StatusCode} in {DurationMs}ms",
                _serviceName,
                method,
                uri,
                statusCodeInt,
                durationMs);
        }
    }
}
