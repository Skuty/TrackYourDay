using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using TrackYourDay.Core.ApplicationTrackers.Breaks;
using TrackYourDay.Core.ApplicationTrackers.MsTeams;
using TrackYourDay.Core.ApplicationTrackers.UserTasks;
using TrackYourDay.Core.Insights.Analytics;
using TrackYourDay.Core.Insights.Workdays;
using TrackYourDay.Core.Notifications;
using TrackYourDay.Core.Settings;
using TrackYourDay.Core.SystemTrackers;
using TrackYourDay.Core.SystemTrackers.ActivityRecognizing;
using TrackYourDay.Core.Versioning;

namespace TrackYourDay.Core.ServiceRegistration
{
    // Narrow this class to conain only core service registrations
    // Now those are udplicated between here and maui project reigstraiton
    // Then register THIS service in WEB project ase core and required
    public static class ServiceCollections
    {
        public static IServiceCollection AddTrackers(this IServiceCollection services)
        {
            services.AddScoped<ISystemStateRecognizingStrategy, FocusedWindowRecognizingStategy>();
            // Refactor to avoid this in future
            services.AddSingleton<ActivityTracker>(container =>
            {
                var clock = container.GetRequiredService<IClock>();
                var publisher = container.GetRequiredService<IPublisher>();
                var focusedWindowRecognizingStategy = new FocusedWindowRecognizingStategy();
                var mousePositionRecognizingStrategy = new MousePositionRecognizingStrategy();
                var lastInputRecognizingStrategy = new LastInputRecognizingStrategy();
                var logger = container.GetRequiredService<ILogger<ActivityTracker>>();

                return new ActivityTracker(
                    clock, 
                    publisher, 
                    focusedWindowRecognizingStategy, 
                    mousePositionRecognizingStrategy, 
                    lastInputRecognizingStrategy, 
                    logger);
            });

            services.AddSingleton<MsTeamsMeetingTracker>(container =>
            {
                var clock = container.GetRequiredService<IClock>();
                var publisher = container.GetRequiredService<IPublisher>();
                var loggerForStrategy = container.GetRequiredService<ILogger<ProcessBasedMeetingRecognizingStrategy>>();
                var meetingDiscoveryStrategy = new ProcessBasedMeetingRecognizingStrategy(loggerForStrategy, new WindowsProcessService());
                var loggerForMs = container.GetRequiredService<ILogger<MsTeamsMeetingTracker>>();
                return new MsTeamsMeetingTracker(clock, publisher, meetingDiscoveryStrategy, loggerForMs);
            });

            var activitiesSettings = ActivitiesSettings.CreateDefaultSettings();
            var breaksSettings = BreaksSettings.CreateDefaultSettings();

            services.AddSingleton<BreakTracker>(serviceCollection => new BreakTracker(
                serviceCollection.GetRequiredService<IPublisher>(),
                serviceCollection.GetRequiredService<IClock>(),
                breaksSettings.TimeOfNoActivityToStartBreak,
                serviceCollection.GetRequiredService<ILogger<BreakTracker>>()));

            services.AddSingleton<ActivitiesAnalyser>();

            services.AddSingleton<UserTaskService>();

            return services;
        }

        public static IServiceCollection AddCoreNotifications(this IServiceCollection services)
        {
            services.AddSingleton<WorkdayReadModelRepository>();
            services.AddSingleton<NotificationRepository>();
            services.AddSingleton<NotificationService>();

            return services;
        }

        public static IServiceCollection AddSettings(this IServiceCollection services)
        {
            services.AddSingleton<IClock, Clock>();
            services.AddSingleton<VersioningSystemFacade, VersioningSystemFacade>();
            services.AddSingleton<SettingsService>();
            services.AddSingleton<ISettingsSet>(serviceProvider =>
                serviceProvider.GetService<SettingsService>().GetCurrentSettingSet());

            return services;
        }
    }
}
