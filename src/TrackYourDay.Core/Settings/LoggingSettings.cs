namespace TrackYourDay.Core.Settings
{
    public class LoggingSettings
    {
        public string MinimumLogLevel { get; set; } = "Information";
        public bool EnablePerClassLogging { get; set; } = true;
        public string LogDirectory { get; set; } = "C:\\Logs\\TrackYourDay";
    }
}
