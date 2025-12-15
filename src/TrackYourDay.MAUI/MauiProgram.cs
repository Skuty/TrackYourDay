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
using TrackYourDay.MAUI.Infrastructure;
using Microsoft.Extensions.Configuration;

namespace TrackYourDay.MAUI
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            try
            {
                // Setup global exception handlers first
                SetupGlobalExceptionHandlers();

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
                Log.Information("TrackYourDay MAUI application initialized successfully");
                return builder.Build();
            }
            catch (Exception ex)
            {
                // Log to a safe location if Serilog isn't configured yet
                LogStartupError(ex);
                throw;
            }
        }

        private static void SetupGlobalExceptionHandlers()
        {
            // Handle unhandled exceptions in the application domain
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                var exception = args.ExceptionObject as Exception;
                LogUnhandledException(exception, "AppDomain.UnhandledException");
            };

            // Handle unobserved task exceptions
            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                LogUnhandledException(args.Exception, "TaskScheduler.UnobservedTaskException");
                args.SetObserved();
            };
        }

        private static void LogStartupError(Exception ex)
        {
            var errorLogPath = GetSafeErrorLogPath();
            try
            {
                var errorMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Startup Error: {ex}\n\n";
                File.AppendAllText(errorLogPath, errorMessage);
            }
            catch
            {
                // If we can't even write to the error log, there's nothing more we can do
            }
        }

        private static void LogUnhandledException(Exception ex, string source)
        {
            try
            {
                Log.Fatal(ex, "Unhandled exception from {Source}", source);
            }
            catch
            {
                // If Serilog fails, fall back to file logging
                LogStartupError(ex);
            }
        }

        private static string GetSafeErrorLogPath()
        {
            // Try multiple locations for error logging
            var locations = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TrackYourDay", "Errors"),
                Path.Combine(Path.GetTempPath(), "TrackYourDay", "Errors"),
                Path.Combine(AppContext.BaseDirectory, "Errors")
            };

            foreach (var location in locations)
            {
                try
                {
                    if (!Directory.Exists(location))
                    {
                        Directory.CreateDirectory(location);
                    }
                    return Path.Combine(location, "startup-errors.log");
                }
                catch
                {
                    continue;
                }
            }

            // Last resort - use temp file
            return Path.Combine(Path.GetTempPath(), "TrackYourDay-startup-errors.log");
        }
    }
}