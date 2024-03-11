using MediatR;
using Microsoft.Extensions.Logging;
using TrackYourDay.Core;
using TrackYourDay.Core.Activities;
using TrackYourDay.Core.Activities.ActivityRecognizing;
using TrackYourDay.Core.Breaks;
using TrackYourDay.Core.Settings;
using TrackYourDay.Core.Versioning;
using TrackYourDay.MAUI.BackgroundJobs;
using TrackYourDay.MAUI.Notifications;

namespace TrackYourDay.MAUI.ServiceRegistration
{
    public static class ServiceCollections
    {
        public static IServiceCollection AddTrackers(this IServiceCollection services)
        {
            services.AddScoped<ISystemStateRecognizingStrategy, DefaultActivityRecognizingStategy>();
            // Refactor to avoid this in future
            services.AddSingleton<ActivityTracker>(container => {
                var clock = container.GetRequiredService<IClock>();
                var publisher = container.GetRequiredService<IPublisher>();
                var startedActivityRecognizingStrategy = new DefaultActivityRecognizingStategy();
                var mousePositionRecognizingStrategy = new MousePositionRecognizingStrategy();
                var logger = container.GetRequiredService<ILogger<ActivityTracker>>();

                return new ActivityTracker(clock, publisher, startedActivityRecognizingStrategy, mousePositionRecognizingStrategy, logger);
            });

            var activitiesSettings = ActivitiesSettings.CreateDefaultSettings();
            var breaksSettings = BreaksSettings.CreateDefaultSettings();

            services.AddSingleton<BreakTracker>(serviceCollection => new BreakTracker(
                serviceCollection.GetRequiredService<IPublisher>(),
                serviceCollection.GetRequiredService<IClock>(),
                breaksSettings.TimeOfNoActivityToStartBreak,
                serviceCollection.GetRequiredService<ILogger<BreakTracker>>()));

            return services;
        }

        public static IServiceCollection AddNotifications(this IServiceCollection services)
        {
            services.AddSingleton<ExecutableNotificationFactory>();
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
