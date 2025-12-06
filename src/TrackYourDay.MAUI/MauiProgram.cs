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

            // Register settings services first to load logging configuration
            builder.Services.AddSingleton<IEncryptionService, EncryptionService>();
            builder.Services.AddSingleton<IClock, Clock>();
            builder.Services.AddSingleton<IGenericSettingsRepository, SqliteGenericSettingsRepository>();
            builder.Services.AddSingleton<IGenericSettingsService, GenericSettingsService>();
            builder.Services.AddSingleton<ILoggingSettingsService, LoggingSettingsService>();

            // Build a temporary service provider to get logging settings
            var tempServiceProvider = builder.Services.BuildServiceProvider();
            var loggingSettingsService = tempServiceProvider.GetRequiredService<ILoggingSettingsService>();
            var loggingSettings = loggingSettingsService.GetLoggingSettings();

            // Configure Serilog with the settings
            Log.Logger = LoggingConfiguration.ConfigureSerilog(loggingSettings);

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
            return builder.Build();
        }
    }
}