namespace TrackYourDay.Core.Settings
{
    public class SettingsService
    {
        private readonly ISettingsRepository settingsRepository;
        private readonly IEncryptionService encryptionService;
        private ISettingsSet currentSettings = null;

        public SettingsService(ISettingsRepository settingsRepository, IEncryptionService encryptionService)
        {
            this.settingsRepository = settingsRepository;
            this.encryptionService = encryptionService;
        }

        public ISettingsSet GetCurrentSettingSet()
        {
            if (this.currentSettings is null)
            {
                var settingsSet = this.settingsRepository.Get();
                var settingsSetWithDecrypterValues = new UserSettingsSet(
                    settingsSet.ActivitiesSettings,
                    settingsSet.BreaksSettings,
                    settingsSet.WorkdayDefinition,
                    new ApplicationTrackers.GitLab.GitLabSettings(
                        this.encryptionService.Decrypt(settingsSet.GitLabSettings.ApiUrl),
                        this.encryptionService.Decrypt(settingsSet.GitLabSettings.ApiKey)
                        ));

                this.currentSettings = settingsSetWithDecrypterValues;
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
                var encryptedSettingsSet = new UserSettingsSet(
                    this.currentSettings.ActivitiesSettings,
                    this.currentSettings.BreaksSettings,
                    this.currentSettings.WorkdayDefinition,
                    new ApplicationTrackers.GitLab.GitLabSettings(
                        this.encryptionService.Encrypt(this.currentSettings.GitLabSettings.ApiUrl),
                        this.encryptionService.Encrypt(this.currentSettings.GitLabSettings.ApiKey)
                        ));
                this.settingsRepository.Save(encryptedSettingsSet);
            }
        }

        public void LoadPersistedSettings()
        {
            this.currentSettings = this.settingsRepository.Get();
            // TODO: Publish notification SettingsChanged
        }
    }
}
