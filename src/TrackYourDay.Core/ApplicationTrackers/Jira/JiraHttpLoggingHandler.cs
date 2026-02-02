using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace TrackYourDay.Core.ApplicationTrackers.Jira;

/// <summary>
/// HTTP message handler that logs Jira API requests and responses.
/// </summary>
public class JiraHttpLoggingHandler : DelegatingHandler
{
    private readonly ILogger<JiraHttpLoggingHandler> _logger;

    public JiraHttpLoggingHandler(ILogger<JiraHttpLoggingHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestUri = request.RequestUri?.ToString() ?? "unknown";

        _logger.LogDebug("Jira HTTP {Method} {Uri}", request.Method, requestUri);

        HttpResponseMessage response;
        try
        {
            response = await base.SendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Jira HTTP {Method} {Uri} failed after {ElapsedMs}ms", 
                request.Method, requestUri, stopwatch.ElapsedMilliseconds);
            throw;
        }

        stopwatch.Stop();

        if (response.IsSuccessStatusCode)
        {
            _logger.LogDebug("Jira HTTP {Method} {Uri} completed with {StatusCode} in {ElapsedMs}ms",
                request.Method, requestUri, (int)response.StatusCode, stopwatch.ElapsedMilliseconds);
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "Jira HTTP {Method} {Uri} failed with {StatusCode} in {ElapsedMs}ms. Response: {Response}",
                request.Method, requestUri, (int)response.StatusCode, stopwatch.ElapsedMilliseconds, errorContent);
        }

        return response;
    }
}
