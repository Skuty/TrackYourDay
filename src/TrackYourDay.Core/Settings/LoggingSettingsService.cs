namespace TrackYourDay.Core.Settings
{
    public class LoggingSettingsService : ILoggingSettingsService
    {
        private const string LoggingSettingsKey = "LoggingSettings";
        private readonly IGenericSettingsService genericSettingsService;

        public LoggingSettingsService(IGenericSettingsService genericSettingsService)
        {
            this.genericSettingsService = genericSettingsService;
        }

        public LoggingSettings GetLoggingSettings()
        {
            return genericSettingsService.GetSetting<LoggingSettings>(LoggingSettingsKey, new LoggingSettings());
        }

        public void SaveLoggingSettings(LoggingSettings settings)
        {
            genericSettingsService.SetSetting(LoggingSettingsKey, settings);
        }
    }
}
