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
        var requestUri = request.RequestUri?.ToString() ?? "unknown";

        _logger.LogDebug("{ServiceName} HTTP {Method} {Uri}", _serviceName, request.Method, requestUri);

        HttpResponseMessage response;
        try
        {
            response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{ServiceName} HTTP {Method} {Uri} failed after {ElapsedMs}ms",
                _serviceName, request.Method, requestUri, stopwatch.ElapsedMilliseconds);
            throw;
        }

        stopwatch.Stop();

        if (response.IsSuccessStatusCode)
        {
            _logger.LogDebug("{ServiceName} HTTP {Method} {Uri} completed with {StatusCode} in {ElapsedMs}ms",
                _serviceName, request.Method, requestUri, (int)response.StatusCode, stopwatch.ElapsedMilliseconds);
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogError(
                "{ServiceName} HTTP {Method} {Uri} failed with {StatusCode} in {ElapsedMs}ms. Response: {Response}",
                _serviceName, request.Method, requestUri, (int)response.StatusCode, stopwatch.ElapsedMilliseconds, errorContent);
        }

        return response;
    }
}
