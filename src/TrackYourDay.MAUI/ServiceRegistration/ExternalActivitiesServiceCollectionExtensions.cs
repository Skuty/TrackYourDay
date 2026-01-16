using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using TrackYourDay.Core.ApplicationTrackers.GitLab;
using TrackYourDay.Core.ApplicationTrackers.Jira;
using TrackYourDay.Core.ApplicationTrackers.Persistence;
using TrackYourDay.MAUI.Infrastructure.Persistence;

namespace TrackYourDay.MAUI.ServiceRegistration
{
    internal static class ExternalActivitiesServiceCollectionExtensions
    {
        private const string DatabasePath = "TrackYourDay.db";

        public static IServiceCollection AddExternalActivitiesPersistence(this IServiceCollection services)
        {
            services.AddSingleton<IGitLabActivityRepository>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<GitLabActivityRepository>>();
                return new GitLabActivityRepository(DatabasePath, logger);
            });

            services.AddSingleton<IJiraActivityRepository>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<JiraActivityRepository>>();
                return new JiraActivityRepository(DatabasePath, logger);
            });

            services.AddSingleton<IJiraIssueRepository>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<JiraIssueRepository>>();
                return new JiraIssueRepository(DatabasePath, logger);
            });

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
