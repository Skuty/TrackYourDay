using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using TrackYourDay.Core.ApplicationTrackers;
using TrackYourDay.Core.ApplicationTrackers.GitLab;
using TrackYourDay.Core.ApplicationTrackers.Jira;
using TrackYourDay.Core.ApplicationTrackers.Persistence;
using TrackYourDay.Core.Persistence;
using TrackYourDay.Infrastructure.Persistence;
using TrackYourDay.MAUI.Infrastructure.Persistence;

namespace TrackYourDay.MAUI.ServiceRegistration
{
    internal static class ExternalActivitiesServiceCollectionExtensions
    {
        public static IServiceCollection AddExternalActivitiesPersistence(this IServiceCollection services)
        {
            services.AddSingleton<IDatabaseKeyProvider, WindowsDatabaseKeyProvider>();
            services.AddSingleton<ISqliteConnectionFactory, SqlCipherConnectionFactory>();
            
            services.AddSingleton<IJiraIssueRepository, JiraIssueRepository>();

            return services;
        }

        public static IServiceCollection AddExternalActivitiesHttpClients(this IServiceCollection services)
        {
            // Do not resolve settings during DI registration to avoid using BuildServiceProvider here;
            // register named HttpClients with deferred configuration at runtime via a factory.

            services.AddHttpClient("GitLab")
                .ConfigureHttpClient((sp, client) =>
                {
                    var gitLabSettings = sp.GetRequiredService<IGitLabSettingsService>().GetSettings();
                    if (!string.IsNullOrEmpty(gitLabSettings.ApiUrl))
                    {
                        client.BaseAddress = new Uri(gitLabSettings.ApiUrl);
                        client.DefaultRequestHeaders.Remove("PRIVATE-TOKEN");
                        if (!string.IsNullOrEmpty(gitLabSettings.ApiKey))
                        {
                            client.DefaultRequestHeaders.Add("PRIVATE-TOKEN", gitLabSettings.ApiKey);
                        }
                        client.Timeout = TimeSpan.FromSeconds(30);
                    }
                })
                .AddPolicyHandler((sp, req) =>
                {
                    var settings = sp.GetRequiredService<IGitLabSettingsService>().GetSettings();
                    return GetCircuitBreakerPolicy(settings.CircuitBreakerThreshold, settings.CircuitBreakerDurationMinutes);
                })
                .AddPolicyHandler((sp, req) => GetRetryPolicy())
                .AddHttpMessageHandler(sp =>
                {
                    var logger = sp.GetRequiredService<ILogger<HttpLoggingHandler>>();
                    return new HttpLoggingHandler(logger, "GitLab");
                });

            services.AddHttpClient("Jira")
                .ConfigureHttpClient((sp, client) =>
                {
                    var jiraSettings = sp.GetRequiredService<IJiraSettingsService>().GetSettings();
                    if (!string.IsNullOrEmpty(jiraSettings.ApiUrl))
                    {
                        client.BaseAddress = new Uri(jiraSettings.ApiUrl);
                        client.DefaultRequestHeaders.Remove("Authorization");
                        if (!string.IsNullOrEmpty(jiraSettings.ApiKey))
                        {
                            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {jiraSettings.ApiKey}");
                        }
                        client.Timeout = TimeSpan.FromSeconds(30);
                    }
                })
                .AddPolicyHandler((sp, req) =>
                {
                    var settings = sp.GetRequiredService<IJiraSettingsService>().GetSettings();
                    return GetCircuitBreakerPolicy(settings.CircuitBreakerThreshold, settings.CircuitBreakerDurationMinutes);
                })
                .AddPolicyHandler((sp, req) => GetRetryPolicy())
                .AddHttpMessageHandler(sp =>
                {
                    var logger = sp.GetRequiredService<ILogger<HttpLoggingHandler>>();
                    return new HttpLoggingHandler(logger, "Jira");
                });

            return services;
        }

        private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(int threshold, int durationMinutes)
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(threshold, TimeSpan.FromMinutes(durationMinutes));
        }

        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        }
    }
}
