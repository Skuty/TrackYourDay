namespace TrackYourDay.Core.SystemTrackers
{
    public interface IActivitiesSettingsService
    {
        /// <summary>
        /// Gets the current activities settings.
        /// </summary>
        /// <returns>The activities settings</returns>
        ActivitiesSettings GetSettings();

        /// <summary>
        /// Updates the frequency of activity discovering.
        /// </summary>
        /// <param name="frequency">The frequency of activity discovering</param>
        void UpdateFrequency(TimeSpan frequency);

        /// <summary>
        /// Persists the activities settings.
        /// </summary>
        void PersistSettings();
    }
}
