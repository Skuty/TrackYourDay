using MediatR;
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

                q.ScheduleJob<NotificationsProcessorJob>(trigger => trigger
                    .WithIdentity("Notifications Processor Job")
                    .WithDescription("Job that periodically checks for notifications to process")
                    .WithDailyTimeIntervalSchedule(x => x.WithInterval(10, IntervalUnit.Second))
                    .StartNow());
            });
        }
    }
}
