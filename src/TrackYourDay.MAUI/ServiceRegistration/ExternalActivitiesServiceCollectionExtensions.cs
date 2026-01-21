using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
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
            using var tempProvider = services.BuildServiceProvider();

            var gitLabSettings = tempProvider.GetRequiredService<IGitLabSettingsService>().GetSettings();
            services.AddHttpClient("GitLab", client =>
            {
                if (!string.IsNullOrEmpty(gitLabSettings.ApiUrl))
                {
                    client.BaseAddress = new Uri(gitLabSettings.ApiUrl);
                    client.DefaultRequestHeaders.Add("PRIVATE-TOKEN", gitLabSettings.ApiKey);
                    client.Timeout = TimeSpan.FromSeconds(30);
                }
            })
            .AddPolicyHandler(GetCircuitBreakerPolicy(gitLabSettings.CircuitBreakerThreshold, gitLabSettings.CircuitBreakerDurationMinutes))
            .AddPolicyHandler(GetRetryPolicy());

            var jiraSettings = tempProvider.GetRequiredService<IJiraSettingsService>().GetSettings();
            services.AddHttpClient("Jira", client =>
            {
                if (!string.IsNullOrEmpty(jiraSettings.ApiUrl))
                {
                    client.BaseAddress = new Uri(jiraSettings.ApiUrl);
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {jiraSettings.ApiKey}");
                    client.Timeout = TimeSpan.FromSeconds(30);
                }
            })
            .AddPolicyHandler(GetCircuitBreakerPolicy(jiraSettings.CircuitBreakerThreshold, jiraSettings.CircuitBreakerDurationMinutes))
            .AddPolicyHandler(GetRetryPolicy());

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
