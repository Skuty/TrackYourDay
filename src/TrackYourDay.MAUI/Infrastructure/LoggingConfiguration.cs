using Serilog;
using Serilog.Core;
using Serilog.Events;
using TrackYourDay.Core.Settings;

namespace TrackYourDay.MAUI.Infrastructure
{
    public static class LoggingConfiguration
    {
        public static ILogger ConfigureSerilog(LoggingSettings settings)
        {
            var logLevel = ParseLogLevel(settings.MinimumLogLevel);
            var logDirectory = settings.LogDirectory;
            
            // Ensure directory exists
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            var loggerConfig = new LoggerConfiguration()
                .MinimumLevel.Is(logLevel)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "TrackYourDay");

            // Add console and debug sinks for development
            loggerConfig
                .WriteTo.Console(restrictedToMinimumLevel: logLevel)
                .WriteTo.Debug(restrictedToMinimumLevel: logLevel);

            // Main application log file
            loggerConfig.WriteTo.File(
                Path.Combine(logDirectory, "TrackYourDay_.log"),
                rollingInterval: RollingInterval.Day,
                restrictedToMinimumLevel: logLevel);

            if (settings.EnablePerClassLogging)
            {
                // Configure generic per-class logging using SourceContext
                // This will create a separate log file for each class that logs
                var perClassDir = Path.Combine(logDirectory, "PerClass");
                if (!Directory.Exists(perClassDir))
                {
                    Directory.CreateDirectory(perClassDir);
                }

                loggerConfig.WriteTo.Logger(lc => lc
                    .Filter.ByIncludingOnly(evt => evt.Properties.ContainsKey("SourceContext"))
                    .WriteTo.Map(
                        evt => evt.Properties.ContainsKey("SourceContext")
                            ? ExtractClassName(evt.Properties["SourceContext"]?.ToString() ?? "Unknown")
                            : "Unknown",
                        (className, wt) => wt.File(
                            Path.Combine(perClassDir, $"{className}_.log"),
                            rollingInterval: RollingInterval.Day,
                            restrictedToMinimumLevel: logLevel)));
            }

            return loggerConfig.CreateLogger();
        }

        private static string ExtractClassName(string sourceContext)
        {
            // SourceContext format is typically: "\"Namespace.ClassName\""
            // Extract just the class name
            var cleaned = sourceContext.Trim('"');
            var lastDot = cleaned.LastIndexOf('.');
            return lastDot >= 0 ? cleaned.Substring(lastDot + 1) : cleaned;
        }

        private static LogEventLevel ParseLogLevel(string logLevel)
        {
            return logLevel.ToLowerInvariant() switch
            {
                "verbose" => LogEventLevel.Verbose,
                "debug" => LogEventLevel.Debug,
                "information" => LogEventLevel.Information,
                "warning" => LogEventLevel.Warning,
                "error" => LogEventLevel.Error,
                "fatal" => LogEventLevel.Fatal,
                _ => LogEventLevel.Information
            };
        }
    }
}
