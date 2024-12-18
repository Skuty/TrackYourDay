﻿using MediatR;
using Microsoft.Extensions.Logging;
using Quartz;
using TrackYourDay.Core;
using TrackYourDay.Core.Activities;
using TrackYourDay.Core.Activities.ActivityRecognizing;
using TrackYourDay.Core.Analytics;
using TrackYourDay.Core.Breaks;
using TrackYourDay.Core.Notifications;
using TrackYourDay.Core.Settings;
using TrackYourDay.Core.UserTasks;
using TrackYourDay.Core.Versioning;
using TrackYourDay.Core.Workdays;
using TrackYourDay.MAUI.BackgroundJobs;
using TrackYourDay.MAUI.BackgroundJobs.ActivityTracking;
using TrackYourDay.MAUI.MauiPages;
using TrackYourDay.MAUI.UiNotifications;

namespace TrackYourDay.MAUI.ServiceRegistration
{
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

        public static IServiceCollection AddNotifications(this IServiceCollection services)
        {
            services.AddSingleton<MauiPageFactory>();
            services.AddSingleton<WorkdayReadModelRepository>();
            services.AddSingleton<INotificationFactory, UiNotificationFactory>();
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

                q.ScheduleJob<NotificationsProcessorJob>(trigger => trigger
                    .WithIdentity("Notifications Processor Job")
                    .WithDescription("Job that periodically checks for notifications to process")
                    .WithDailyTimeIntervalSchedule(x => x.WithInterval(10, IntervalUnit.Second))
                    .StartNow());
            });
        }
    }
}
