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
using TrackYourDay.Core.ServiceRegistration;
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

            builder.Services.AddSingleton(Assembly.GetExecutingAssembly().GetName().Version);

            // Merge this later into Settings
            builder.Services.AddSingleton<ISettingsRepository, SqlLiteSettingsRepository>();
            builder.Services.AddSettings();

            builder.Services.AddTrackers();

            builder.Services.AddCoreNotifications();
            builder.Services.AddMauiNotifications();

            builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<ActivityTracker>());

            builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<AddActivityToProcessWhenActivityStartedEventHandler>());

            builder.Services.AddBackgroundJobs();

            builder.Services.AddQuartzHostedService();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();

            // TODO: This deletes repository if needed, normally its not visible in file explorer on windws 10
            new SqlLiteSettingsRepository().Reset();
#endif
            return builder.Build();
        }
    }
}