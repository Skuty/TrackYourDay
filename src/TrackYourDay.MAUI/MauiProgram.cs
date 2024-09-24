using TrackYourDay.Core.Activities;
using Quartz;
using Microsoft.Maui.LifecycleEvents;
using Serilog;
using MudBlazor.Services;
using System.Reflection;
using TrackYourDay.MAUI.BackgroundJobs.ActivityTracking;
using TrackYourDay.MAUI.BackgroundJobs.BreakTracking;
using TrackYourDay.MAUI.BackgroundJobs;
using TrackYourDay.MAUI.ServiceRegistration;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using TrackYourDay.Core.Settings;
using TrackYourDay.Core.Analytics;
using Microsoft.Extensions.Logging;

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

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .WriteTo.File("C:\\Logs\\TrackYourDay\\TrackYourDay_.log", 
                    rollingInterval: RollingInterval.Day)
                .CreateLogger();

            builder.Services.AddLogging(loggingBuilder =>
                loggingBuilder.AddSerilog(dispose: true));

            builder.Logging.SetMinimumLevel(LogLevel.Warning);

            builder.Services.AddSingleton(Assembly.GetExecutingAssembly().GetName().Version);

            builder.Services.AddSettings();

            builder.Services.AddTrackers();

            builder.Services.AddNotifications();

            builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<ActivityTracker>());

            builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<AddActivityToProcessWhenActivityStartedEventHandler>());

            builder.Services.AddBackgroundJobs();

            builder.Services.AddQuartzHostedService();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();

            // TODO: This deletes repository if needed, normally its not visible in file explorer on windws 10
            new SqlLiteSettingsRepository().Reset();
#endif

            // https://learn.microsoft.com/en-us/answers/questions/1336207/how-to-remove-close-and-maximize-button-for-a-maui?cid=kerryherger
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

            return builder.Build();
        }
    }
}