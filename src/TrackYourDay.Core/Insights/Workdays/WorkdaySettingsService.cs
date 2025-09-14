using TrackYourDay.Core.Settings;

namespace TrackYourDay.Core.Insights.Workdays
{
    public class WorkdaySettingsService : IWorkdaySettingsService
    {
        private const string WORKDAY_DEFINITION_KEY = "Workday.Definition";

        private readonly IGenericSettingsService settingsService;

        public WorkdaySettingsService(IGenericSettingsService settingsService)
        {
            this.settingsService = settingsService;
        }

        public WorkdayDefinition GetWorkdayDefinition()
        {
            return settingsService.GetSetting(WORKDAY_DEFINITION_KEY, WorkdayDefinition.CreateDefaultDefinition());
        }

        public void UpdateWorkdayDefinition(WorkdayDefinition workdayDefinition)
        {
            settingsService.SetSetting(WORKDAY_DEFINITION_KEY, workdayDefinition);
        }

        public void PersistSettings()
        {
            settingsService.PersistSettings();
        }
    }
}
