using Quartz;
using TrackYourDay.Core.ApplicationTrackers.GitLab;
using TrackYourDay.Core.ApplicationTrackers.Jira;
using TrackYourDay.Core.Notifications;
using TrackYourDay.Core.SystemTrackers;
using TrackYourDay.MAUI.BackgroundJobs;
using TrackYourDay.MAUI.BackgroundJobs.ActivityTracking;
using TrackYourDay.MAUI.BackgroundJobs.ExternalActivities;
using TrackYourDay.MAUI.MauiPages;
using TrackYourDay.MAUI.UiNotifications;

namespace TrackYourDay.MAUI.ServiceRegistration
{
    public static class ServiceCollections
    {
        public static IServiceCollection AddMauiNotifications(this IServiceCollection services)
        {
            services.AddSingleton<MauiPageFactory>();
            services.AddSingleton<INotificationFactory, UiNotificationFactory>();

            return services;
        }

        public static IServiceCollection AddBackgroundJobs(this IServiceCollection services)
        {
            return services.AddQuartz(q =>
            {
                q.UseMicrosoftDependencyInjectionJobFactory();

                q.ScheduleJob<ActivityEventTrackerJob>(trigger => trigger
                    .WithIdentity("Activity Recognizing Job")
                    .WithDescription("Job that periodically recognizes user activities")
                    .WithDailyTimeIntervalSchedule(x => x.WithInterval((int)ActivitiesSettings.CreateDefaultSettings().FrequencyOfActivityDiscovering.TotalSeconds, IntervalUnit.Second))
                    .StartNow());

                q.ScheduleJob<MsTeamsMeetingsTrackerJob>(trigger => trigger
                    .WithIdentity("MS Teams Meetings Recognizing Job")
                    .WithDescription("Job that periodically recognizes Meetings in MS Teams")
                    .WithDailyTimeIntervalSchedule(x => x.WithInterval(30, IntervalUnit.Second))
                    .StartNow());

                q.ScheduleJob<NotificationsProcessorJob>(trigger => trigger
                    .WithIdentity("Notifications Processor Job")
                    .WithDescription("Job that periodically checks for notifications to process")
                    .WithDailyTimeIntervalSchedule(x => x.WithInterval(10, IntervalUnit.Second))
                    .StartNow());

                ConfigureExternalActivityJobs(q, services);
            });
        }

        private static void ConfigureExternalActivityJobs(IServiceCollectionQuartzConfigurator q, IServiceCollection services)
        {
            using var tempProvider = services.BuildServiceProvider();
            
            var gitLabSettings = tempProvider.GetRequiredService<IGitLabSettingsService>().GetSettings();
            if (gitLabSettings.Enabled && !string.IsNullOrEmpty(gitLabSettings.ApiUrl))
            {
                q.AddJob<GitLabFetchJob>(opts => opts.WithIdentity("GitLabFetch", "ExternalActivities"));
                q.AddTrigger(opts => opts
                    .ForJob("GitLabFetch", "ExternalActivities")
                    .WithIdentity("GitLabFetchTrigger", "ExternalActivities")
                    .WithSimpleSchedule(x => x
                        .WithIntervalInMinutes(gitLabSettings.FetchIntervalMinutes)
                        .RepeatForever())
                    .StartNow());
            }

            var jiraSettings = tempProvider.GetRequiredService<IJiraSettingsService>().GetSettings();
            if (jiraSettings.Enabled && !string.IsNullOrEmpty(jiraSettings.ApiUrl))
            {
                q.AddJob<JiraFetchJob>(opts => opts.WithIdentity("JiraFetch", "ExternalActivities"));
                q.AddTrigger(opts => opts
                    .ForJob("JiraFetch", "ExternalActivities")
                    .WithIdentity("JiraFetchTrigger", "ExternalActivities")
                    .WithSimpleSchedule(x => x
                        .WithIntervalInMinutes(jiraSettings.FetchIntervalMinutes)
                        .RepeatForever())
                    .StartNow());
            }
        }
    }
}
