using TrackYourDay.Core.Settings;

namespace TrackYourDay.Core.ApplicationTrackers.Breaks
{
    public class BreaksSettingsService : IBreaksSettingsService
    {
        private const string TIME_OF_NO_ACTIVITY_KEY = "Breaks.TimeOfNoActivityToStartBreak";

        private readonly IGenericSettingsService settingsService;

        public BreaksSettingsService(IGenericSettingsService settingsService)
        {
            this.settingsService = settingsService;
        }

        public BreaksSettings GetSettings()
        {
            var timeOfNoActivity = settingsService.GetSetting(TIME_OF_NO_ACTIVITY_KEY, TimeSpan.FromMinutes(5));
            return new BreaksSettings(timeOfNoActivity);
        }

        public void UpdateTimeOfNoActivityToStartBreak(TimeSpan timeOfNoActivityToStartBreak)
        {
            settingsService.SetSetting(TIME_OF_NO_ACTIVITY_KEY, timeOfNoActivityToStartBreak);
        }

        public void PersistSettings()
        {
            settingsService.PersistSettings();
        }
    }
}
