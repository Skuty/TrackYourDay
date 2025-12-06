namespace TrackYourDay.Core.Settings
{
    public class LoggingSettings
    {
        public string MinimumLogLevel { get; set; } = "Information";
        public bool EnablePerClassLogging { get; set; } = true;
        public string LogDirectory { get; set; } = GetDefaultLogDirectory();
        
        private static string GetDefaultLogDirectory()
        {
            // Use platform-agnostic approach for log directory
            if (OperatingSystem.IsWindows())
            {
                return Path.Combine("C:", "Logs", "TrackYourDay");
            }
            else if (OperatingSystem.IsMacOS())
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Logs", "TrackYourDay");
            }
            else // Linux and other Unix-like systems
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "TrackYourDay", "logs");
            }
        }
    }
}
