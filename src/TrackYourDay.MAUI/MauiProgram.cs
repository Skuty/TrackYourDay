using Quartz;
using Microsoft.Maui.LifecycleEvents;
using Serilog;
using MudBlazor.Services;
using System.Reflection;
using TrackYourDay.MAUI.BackgroundJobs.BreakTracking;
using TrackYourDay.MAUI.ServiceRegistration;
using TrackYourDay.Core.ServiceRegistration;
using TrackYourDay.Web.ServiceRegistration;
using TrackYourDay.Core.SystemTrackers;
using Microsoft.Extensions.Configuration;
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

            // Load configuration from appsettings.json
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            // Configure logging from appsettings.json
            Log.Logger = LoggingConfiguration.ConfigureSerilog(configuration);

            builder.Services.AddLogging(loggingBuilder =>
                loggingBuilder.AddSerilog(dispose: true));

            builder.Services.AddSingleton(Assembly.GetExecutingAssembly().GetName().Version);

            // Register database encryption services first (required by Settings and Repositories)
            builder.Services.AddExternalActivitiesPersistence();

            builder.Services.AddSettings();

            builder.Services.AddRepositories();

            builder.Services.AddExternalActivitiesHttpClients();

            builder.Services.AddTrackers();

            builder.Services.AddLlmPromptServices();

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