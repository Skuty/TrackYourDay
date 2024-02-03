namespace TrackYourDay.Core.Settings
{
    public class SettingsService
    {
        private ISettingsSet currentSettings;

        public SettingsService()
        {
            this.currentSettings = new DefaultSettingsSet();    
        }

        public ISettingsSet GetCurrentSettingSet()
        {
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
            // TODO: Persist settings in local SqlLte DB
        }

        public void LoadPersistedSettings()
        {
            // TODO: Load settings from local DB
            this.currentSettings = new DefaultSettingsSet();
            // TODO: Publish notification SettingsChanged
        }
    }
}
