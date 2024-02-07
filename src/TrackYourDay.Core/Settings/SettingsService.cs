namespace TrackYourDay.Core.Settings
{
    public class SettingsService
    {
        private readonly ISettingsRepository settingsRepository;
        private ISettingsSet currentSettings = null;

        public SettingsService(ISettingsRepository settingsRepository)
        {
            this.settingsRepository = settingsRepository;
        }

        public ISettingsSet GetCurrentSettingSet()
        {
            if (this.currentSettings is null)
            {
                this.currentSettings = this.settingsRepository.Get();
            }

            return this.currentSettings;
        }

        public void SetTimeOfNoActivityToStartBreak(TimeSpan timeOfNoActivityToStartBreak)
        {
            this.currentSettings = new UserSettingsSet()
            {
                // TODO Check and use Records createBasedOn feature
                ActivitiesSettings = this.currentSettings.ActivitiesSettings,
                BreaksSettings = new Breaks.BreaksSettings(timeOfNoActivityToStartBreak),
                WorkdayDefinition= this.currentSettings.WorkdayDefinition,
            };

            // TODO: Publish notification SettingsChanged
        }

        public void PersistSettings()
        {
            if (this.currentSettings is not null)
            {
                this.settingsRepository.Save(this.currentSettings);
            }
        }

        public void LoadPersistedSettings()
        {
            this.currentSettings = this.settingsRepository.Get();
            // TODO: Publish notification SettingsChanged
        }
    }
}
