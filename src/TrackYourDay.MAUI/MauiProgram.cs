using Quartz;
using Microsoft.Maui.LifecycleEvents;
using Serilog;
using MudBlazor.Services;
using System.Reflection;
using TrackYourDay.MAUI.BackgroundJobs.BreakTracking;
using TrackYourDay.MAUI.ServiceRegistration;
using TrackYourDay.Core.ServiceRegistration;
using TrackYourDay.Core.Settings;
using TrackYourDay.Web.ServiceRegistration;
using TrackYourDay.Core.SystemTrackers;
using TrackYourDay.MAUI.Infrastructure;

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

            // Configure default logging first (will be reconfigured after loading settings)
            Log.Logger = LoggingConfiguration.ConfigureSerilog(new LoggingSettings());

            builder.Services.AddLogging(loggingBuilder =>
                loggingBuilder.AddSerilog(dispose: true));

            builder.Services.AddSingleton(Assembly.GetExecutingAssembly().GetName().Version);

            builder.Services.AddSettings();

            builder.Services.AddRepositories();

            builder.Services.AddTrackers();

            builder.Services.AddCoreNotifications();

            builder.Services.AddMauiNotifications();

            builder.Services.AddEventHandlingForBlazorUIComponents();

            builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<ActivityTracker>());

            builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<AddActivityToProcessWhenActivityStartedEventHandler>());

            builder.Services.AddBackgroundJobs();

            builder.Services.AddQuartzHostedService();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();

#endif
            var app = builder.Build();
            
            // Reconfigure logging with user settings after DI container is built
            try
            {
                var loggingSettingsService = app.Services.GetRequiredService<ILoggingSettingsService>();
                var loggingSettings = loggingSettingsService.GetLoggingSettings();
                Log.Logger = LoggingConfiguration.ConfigureSerilog(loggingSettings);
            }
            catch
            {
                // If loading settings fails, continue with default configuration
            }
            
            return app;
        }
    }
}