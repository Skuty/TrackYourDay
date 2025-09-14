using TrackYourDay.Core.Settings;

namespace TrackYourDay.Core.SystemTrackers
{
    public class ActivitiesSettingsService : IActivitiesSettingsService
    {
        private const string FREQUENCY_KEY = "Activities.FrequencyOfActivityDiscovering";

        private readonly IGenericSettingsService settingsService;

        public ActivitiesSettingsService(IGenericSettingsService settingsService)
        {
            this.settingsService = settingsService;
        }

        public ActivitiesSettings GetSettings()
        {
            var frequency = settingsService.GetSetting(FREQUENCY_KEY, TimeSpan.FromSeconds(5));
            return new ActivitiesSettings(frequency);
        }

        public void UpdateFrequency(TimeSpan frequency)
        {
            settingsService.SetSetting(FREQUENCY_KEY, frequency);
        }

        public void PersistSettings()
        {
            settingsService.PersistSettings();
        }
    }
}
