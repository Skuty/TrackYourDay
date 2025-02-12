using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TrackYourDay.Core.Activities;
using TrackYourDay.Core.Activities.ActivityRecognizing;
using TrackYourDay.Core.Analytics;
using TrackYourDay.Core.Breaks;
using TrackYourDay.Core.Notifications;
using TrackYourDay.Core.Settings;
using TrackYourDay.Core.UserTasks;
using TrackYourDay.Core.Versioning;
using TrackYourDay.Core.Workdays;

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
            services.AddSingleton<ISettingsRepository, SqlLiteSettingsRepository>();
            services.AddSingleton<SettingsService>();
            services.AddSingleton<ISettingsSet>(serviceProvider =>
                serviceProvider.GetService<SettingsService>().GetCurrentSettingSet());

            return services;
        }
    }
}
