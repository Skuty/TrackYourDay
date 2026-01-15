using Microsoft.Extensions.Logging;
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
            // TODO: Add Polly circuit breaker and retry policies
            // Requires: dotnet add package Microsoft.Extensions.Http.Polly
            // Circuit breaker: 5 failures -> 5 min break
            // Retry: 3 attempts with exponential backoff
            
            services.AddHttpClient("GitLab", (sp, client) =>
            {
                var settingsService = sp.GetRequiredService<IGitLabSettingsService>();
                var settings = settingsService.GetSettings();
                
                if (!string.IsNullOrEmpty(settings.ApiUrl))
                {
                    client.BaseAddress = new Uri(settings.ApiUrl);
                    client.DefaultRequestHeaders.Add("PRIVATE-TOKEN", settings.ApiKey);
                    client.Timeout = TimeSpan.FromSeconds(30);
                }
            });

            services.AddHttpClient("Jira", (sp, client) =>
            {
                var settingsService = sp.GetRequiredService<IJiraSettingsService>();
                var settings = settingsService.GetSettings();
                
                if (!string.IsNullOrEmpty(settings.ApiUrl))
                {
                    client.BaseAddress = new Uri(settings.ApiUrl);
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {settings.ApiKey}");
                    client.Timeout = TimeSpan.FromSeconds(30);
                }
            });

            return services;
        }
    }
}
