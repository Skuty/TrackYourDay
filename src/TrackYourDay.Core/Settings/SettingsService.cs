﻿namespace TrackYourDay.Core.Settings
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
            this.currentSettings = new UserSettingsSet(
                this.currentSettings.ActivitiesSettings, 
                new ApplicationTrackers.Breaks.BreaksSettings(timeOfNoActivityToStartBreak), 
                this.currentSettings.WorkdayDefinition,
                this.currentSettings.GitLabSettings);

            // TODO: Publish notification SettingsChanged
        }

        public void SetGitLabApiUrlAndKey(string url, string key)
        {
            this.currentSettings = new UserSettingsSet(
                this.currentSettings.ActivitiesSettings,
                this.currentSettings.BreaksSettings,
                this.currentSettings.WorkdayDefinition,
                new ApplicationTrackers.GitLab.GitLabSettings(url, key));
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
