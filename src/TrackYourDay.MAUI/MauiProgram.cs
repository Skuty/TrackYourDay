using MediatR;
using Microsoft.Extensions.Logging;
using TrackYourDay.Core.Activities;
using TrackYourDay.Core.Breaks;
using TrackYourDay.Core;
using TrackYourDay.MAUI.Data;
using Quartz;
using TrackYourDay.MAUI.BackgroundJobs;
using Microsoft.Maui.LifecycleEvents;
using Serilog;
using Serilog.Events;
using MudBlazor.Services;
using TrackYourDay.Core.Activities.ActivityRecognizing;
using System.Reflection;
using TrackYourDay.MAUI.Versioning;

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
            builder.Services.AddMudServices();
            builder.Services.AddMauiBlazorWebView();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
#endif
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .WriteTo.File("C:\\Logs\\TrackYourDay\\TrackYourDay_.log", 
                    rollingInterval: RollingInterval.Day)
                .CreateLogger();

            builder.Services.AddLogging(loggingBuilder =>
                loggingBuilder.AddSerilog(dispose: true)); 
            
            builder.Services.AddSingleton<WeatherForecastService>();
            builder.Services.AddSingleton<VersioningSystemFacade, VersioningSystemFacade>();
            builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<ActivityTracker>());
            builder.Services.AddSingleton<IClock, Clock>();
            builder.Services.AddScoped<ISystemStateRecognizingStrategy, DefaultActivityRecognizingStategy>();
            // Refactor to avoid this in future
            builder.Services.AddSingleton<ActivityTracker>(container => {
                var clock = container.GetRequiredService<IClock>();
                var publisher = container.GetRequiredService<IPublisher>();
                var startedActivityRecognizingStrategy = new DefaultActivityRecognizingStategy();
                var mousePositionRecognizingStrategy = new MousePositionRecognizingStrategy();
                var logger = container.GetRequiredService<ILogger<ActivityTracker>>();

                return new ActivityTracker(clock, publisher, startedActivityRecognizingStrategy, mousePositionRecognizingStrategy, logger);
            });
            builder.Services.AddSingleton<BreakTracker>(serviceCollection => new BreakTracker(
                serviceCollection.GetRequiredService<IPublisher>(),
                serviceCollection.GetRequiredService<IClock>(),
                Config.TimeOfNoActivityToStartBreak,
                serviceCollection.GetRequiredService<ILogger<BreakTracker>>()));
            // Install notification handler
            builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<ActivityStartedNotificationHandler>());

            builder.Services.AddQuartz(q =>
            {
                q.UseMicrosoftDependencyInjectionJobFactory();
                q.ScheduleJob<ActivityEventTrackerJob>(trigger => trigger
                    .WithIdentity("Activity Recognizing Job")
                    .WithDescription("Job that periodically recognizes user activities")
                    .WithDailyTimeIntervalSchedule(x => x.WithInterval((int)Config.FrequencyOfActivityDiscovering.TotalSeconds, IntervalUnit.Second))
                    .StartNow());
            });

            builder.Services.AddQuartzHostedService();



            // https://learn.microsoft.com/en-us/answers/questions/1336207/how-to-remove-close-and-maximize-button-for-a-maui?cid=kerryherger
#if WINDOWS
            builder.ConfigureLifecycleEvents(events =>
            {
                // Make sure to add "using Microsoft.Maui.LifecycleEvents;" in the top of the file
                events.AddWindows(windowsLifecycleBuilder =>
                {
                    windowsLifecycleBuilder.OnWindowCreated(window =>
                    {
                        window.ExtendsContentIntoTitleBar = false;
                        var handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
                        var id = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(handle);
                        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(id);

                        switch (appWindow.Presenter)
                        {
                            case Microsoft.UI.Windowing.OverlappedPresenter overlappedPresenter:
                                //disable the max button
                                overlappedPresenter.IsMaximizable = true;
                                overlappedPresenter.Maximize();
                                break;
                        }

                        //When user execute the closing method, we can make the window do not close by   e.Cancel = true;.
                        appWindow.Closing += async (s, e) =>
                        {
                            e.Cancel = true;
                            switch (appWindow.Presenter)
                            {
                                case Microsoft.UI.Windowing.OverlappedPresenter overlappedPresenter:
                                    overlappedPresenter.Minimize();
                                    break;
                            }

                        };
                    });
                });
            });
#endif
            return builder.Build();
        }
    }
}