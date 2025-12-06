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
                // Configure per-class logging for trackers
                AddPerClassLogger(loggerConfig, logDirectory, "ActivityTracker", logLevel);
                AddPerClassLogger(loggerConfig, logDirectory, "BreakTracker", logLevel);
                AddPerClassLogger(loggerConfig, logDirectory, "MsTeamsMeetingTracker", logLevel);
                AddPerClassLogger(loggerConfig, logDirectory, "GitLabTracker", logLevel);
                AddPerClassLogger(loggerConfig, logDirectory, "JiraTracker", logLevel);
                
                // Configure per-class logging for analytics/summary strategies
                AddPerClassLogger(loggerConfig, logDirectory, "ActivitiesAnalyser", logLevel);
                AddPerClassLogger(loggerConfig, logDirectory, "MLNetSummaryStrategy", logLevel);
                AddPerClassLogger(loggerConfig, logDirectory, "JiraEnrichedSummaryStrategy", logLevel);
                AddPerClassLogger(loggerConfig, logDirectory, "HybridContextualSummaryStrategy", logLevel);
                AddPerClassLogger(loggerConfig, logDirectory, "ContextBasedSummaryStrategy", logLevel);
                AddPerClassLogger(loggerConfig, logDirectory, "TimeBasedSummaryStrategy", logLevel);
                AddPerClassLogger(loggerConfig, logDirectory, "DurationBasedSummaryStrategy", logLevel);
                AddPerClassLogger(loggerConfig, logDirectory, "JiraKeySummaryStrategy", logLevel);
                
                // Configure per-class logging for persistence
                AddPerClassLogger(loggerConfig, logDirectory, "PersistEndedActivityHandler", logLevel);
                AddPerClassLogger(loggerConfig, logDirectory, "PersistEndedBreakHandler", logLevel);
                AddPerClassLogger(loggerConfig, logDirectory, "PersistEndedMeetingHandler", logLevel);
            }

            return loggerConfig.CreateLogger();
        }

        private static void AddPerClassLogger(
            LoggerConfiguration config, 
            string logDirectory, 
            string className, 
            LogEventLevel minLevel)
        {
            var logFile = Path.Combine(logDirectory, "PerClass", $"{className}_.log");
            
            // Ensure per-class directory exists
            var perClassDir = Path.Combine(logDirectory, "PerClass");
            if (!Directory.Exists(perClassDir))
            {
                Directory.CreateDirectory(perClassDir);
            }

            config.WriteTo.Logger(lc => lc
                .Filter.ByIncludingOnly(evt => 
                {
                    if (!evt.Properties.ContainsKey("SourceContext"))
                        return false;
                    
                    var sourceContext = evt.Properties["SourceContext"]?.ToString() ?? string.Empty;
                    // Use EndsWith to match the class name more precisely and avoid false positives
                    return sourceContext.EndsWith($"{className}\"", StringComparison.OrdinalIgnoreCase);
                })
                .WriteTo.File(
                    logFile,
                    rollingInterval: RollingInterval.Day,
                    restrictedToMinimumLevel: minLevel));
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
