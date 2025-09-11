namespace TrackYourDay.Core.Insights.Workdays
{
    public interface IWorkdaySettingsService
    {
        /// <summary>
        /// Gets the current workday definition.
        /// </summary>
        /// <returns>The workday definition</returns>
        WorkdayDefinition GetWorkdayDefinition();

        /// <summary>
        /// Updates the workday definition.
        /// </summary>
        /// <param name="workdayDefinition">The new workday definition</param>
        void UpdateWorkdayDefinition(WorkdayDefinition workdayDefinition);

        /// <summary>
        /// Persists the workday settings.
        /// </summary>
        void PersistSettings();
    }
}
