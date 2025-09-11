using TrackYourDay.Core.ApplicationTrackers.Breaks;
using TrackYourDay.Core.ApplicationTrackers.GitLab;
using TrackYourDay.Core.ApplicationTrackers.Jira;
using TrackYourDay.Core.Insights.Workdays;
using TrackYourDay.Core.SystemTrackers;

namespace TrackYourDay.Core.Settings
{
    public class SettingsMigrationService
    {
        private readonly ISettingsRepository legacyRepository;
        private readonly IBreaksSettingsService breaksSettingsService;
        private readonly IGitLabSettingsService gitLabSettingsService;
        private readonly IJiraSettingsService jiraSettingsService;
        private readonly IActivitiesSettingsService activitiesSettingsService;
        private readonly IWorkdaySettingsService workdaySettingsService;

        public SettingsMigrationService(
            ISettingsRepository legacyRepository,
            IBreaksSettingsService breaksSettingsService,
            IGitLabSettingsService gitLabSettingsService,
            IJiraSettingsService jiraSettingsService,
            IActivitiesSettingsService activitiesSettingsService,
            IWorkdaySettingsService workdaySettingsService)
        {
            this.legacyRepository = legacyRepository;
            this.breaksSettingsService = breaksSettingsService;
            this.gitLabSettingsService = gitLabSettingsService;
            this.jiraSettingsService = jiraSettingsService;
            this.activitiesSettingsService = activitiesSettingsService;
            this.workdaySettingsService = workdaySettingsService;
        }

        /// <summary>
        /// Migrates settings from the legacy monolithic format to the new generic format.
        /// </summary>
        public void MigrateLegacySettings()
        {
            try
            {
                var legacySettings = legacyRepository.Get();
                
                if (legacySettings == null)
                {
                    return; // No legacy settings to migrate
                }

                // Migrate breaks settings
                if (legacySettings.BreaksSettings != null)
                {
                    breaksSettingsService.UpdateTimeOfNoActivityToStartBreak(
                        legacySettings.BreaksSettings.TimeOfNoActivityToStartBreak);
                    breaksSettingsService.PersistSettings();
                }

                // Migrate GitLab settings
                if (legacySettings.GitLabSettings != null)
                {
                    gitLabSettingsService.UpdateSettings(
                        legacySettings.GitLabSettings.ApiUrl,
                        legacySettings.GitLabSettings.ApiKey);
                    gitLabSettingsService.PersistSettings();
                }

                // Migrate Jira settings
                if (legacySettings.JiraSettings != null)
                {
                    jiraSettingsService.UpdateSettings(
                        legacySettings.JiraSettings.ApiUrl,
                        legacySettings.JiraSettings.ApiKey);
                    jiraSettingsService.PersistSettings();
                }

                // Migrate activities settings
                if (legacySettings.ActivitiesSettings != null)
                {
                    activitiesSettingsService.UpdateFrequency(
                        legacySettings.ActivitiesSettings.FrequencyOfActivityDiscovering);
                    activitiesSettingsService.PersistSettings();
                }

                // Migrate workday settings
                if (legacySettings.WorkdayDefinition != null)
                {
                    workdaySettingsService.UpdateWorkdayDefinition(legacySettings.WorkdayDefinition);
                    workdaySettingsService.PersistSettings();
                }
            }
            catch (Exception ex)
            {
                // Log the exception but don't fail the application startup
                // In a real application, you would use proper logging here
                Console.WriteLine($"Failed to migrate legacy settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if legacy settings exist and need migration.
        /// </summary>
        public bool HasLegacySettings()
        {
            try
            {
                var legacySettings = legacyRepository.Get();
                return legacySettings != null && !(legacySettings is DefaultSettingsSet);
            }
            catch
            {
                return false;
            }
        }
    }
}
