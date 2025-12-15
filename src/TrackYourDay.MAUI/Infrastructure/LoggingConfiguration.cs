using Serilog;
using Serilog.Core;
using Serilog.Events;
using Microsoft.Extensions.Configuration;

namespace TrackYourDay.MAUI.Infrastructure
{
    public static class LoggingConfiguration
    {
        public static ILogger ConfigureSerilog(IConfiguration configuration)
        {
            var loggingSection = configuration.GetSection("Logging");
            var logLevel = ParseLogLevel(loggingSection["MinimumLogLevel"] ?? "Information");
            var configuredLogDirectory = loggingSection["LogDirectory"] ?? "C:\\Logs\\TrackYourDay";
            var enablePerClassLogging = loggingSection.GetValue<bool>("EnablePerClassLogging", true);
            
            // Try to create the configured log directory, fall back to safer locations if it fails
            string logDirectory = GetOrCreateLogDirectory(configuredLogDirectory);

            var loggerConfig = new LoggerConfiguration()
                .MinimumLevel.Is(logLevel)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "TrackYourDay")
                .Enrich.WithProperty("LogDirectory", logDirectory);

            // Add console and debug sinks for development
            loggerConfig
                .WriteTo.Console(restrictedToMinimumLevel: logLevel)
                .WriteTo.Debug(restrictedToMinimumLevel: logLevel);

            // Main application log file
            try
            {
                loggerConfig.WriteTo.File(
                    Path.Combine(logDirectory, "TrackYourDay_.log"),
                    rollingInterval: RollingInterval.Day,
                    restrictedToMinimumLevel: logLevel);
            }
            catch (Exception ex)
            {
                // If file logging fails, at least log to console
                Console.WriteLine($"Failed to configure file logging: {ex.Message}");
            }

            if (enablePerClassLogging)
            {
                try
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
                catch (Exception ex)
                {
                    // If per-class logging fails, log the error but continue
                    Console.WriteLine($"Failed to configure per-class logging: {ex.Message}");
                }
            }

            return loggerConfig.CreateLogger();
        }

        private static string GetOrCreateLogDirectory(string preferredDirectory)
        {
            // Try multiple locations in order of preference
            var candidateDirectories = new[]
            {
                preferredDirectory,
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TrackYourDay", "Logs"),
                Path.Combine(Path.GetTempPath(), "TrackYourDay", "Logs"),
                Path.Combine(AppContext.BaseDirectory, "Logs")
            };

            foreach (var directory in candidateDirectories)
            {
                try
                {
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    
                    // Test if we can write to this directory
                    var testFile = Path.Combine(directory, ".write-test");
                    try
                    {
                        File.WriteAllText(testFile, "test");
                        File.Delete(testFile);
                    }
                    catch
                    {
                        // Clean up test file if delete fails
                        try { File.Delete(testFile); } catch { }
                        throw;
                    }
                    
                    return directory;
                }
                catch
                {
                    // Try next directory
                    continue;
                }
            }

            // Last resort - use temp directory directly
            return Path.GetTempPath();
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
