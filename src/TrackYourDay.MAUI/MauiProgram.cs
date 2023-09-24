using MediatR;
using Microsoft.Extensions.Logging;
using TrackYourDay.Core.Activities;
using TrackYourDay.Core.Breaks;
using TrackYourDay.Core.Old.Activities.RecognizingStrategies;
using TrackYourDay.Core;
using TrackYourDay.MAUI.Data;
using Quartz;
using TrackYourDay.MAUI.BackgroundJobs;

namespace TrackYourDay.MAUI
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });
            builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif
            
            builder.Services.AddSingleton<WeatherForecastService>();
            builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<ActivityTracker>());
            builder.Services.AddSingleton<IClock, Clock>();
            builder.Services.AddScoped<IStartedActivityRecognizingStrategy, DefaultActivityRecognizingStategy>();
            builder.Services.AddScoped<IInstantActivityRecognizingStrategy, DefaultInstantActivityRecognizingStrategy>();
            builder.Services.AddSingleton<ActivityTracker>();
            builder.Services.AddSingleton<BreakTracker>(serviceCollection => new BreakTracker(
                serviceCollection.GetRequiredService<IPublisher>(),
                serviceCollection.GetRequiredService<IClock>(),
                TimeSpan.FromMinutes(5)));
            // Install notification handler
            builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<ActivityStartedNotificationHandler>());

            builder.Services.AddQuartz(q =>
            {
                q.UseMicrosoftDependencyInjectionJobFactory();
                q.ScheduleJob<ActivityEventTrackerJob>(trigger => trigger
                    .WithIdentity("Activity Recognizing Job")
                    .WithDescription("Job that periodically recognizes user activities")
                    .WithDailyTimeIntervalSchedule(x => x.WithInterval(10, IntervalUnit.Second))
                    .StartNow());
            });

            builder.Services.AddQuartzHostedService();


            return builder.Build();
        }
    }
}