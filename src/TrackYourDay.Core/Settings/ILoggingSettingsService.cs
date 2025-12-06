namespace TrackYourDay.Core.Settings
{
    public interface ILoggingSettingsService
    {
        LoggingSettings GetLoggingSettings();
        void SaveLoggingSettings(LoggingSettings settings);
    }
}
