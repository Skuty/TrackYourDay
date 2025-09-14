namespace TrackYourDay.Core.ApplicationTrackers.Breaks
{
    public interface IBreaksSettingsService
    {
        /// <summary>
        /// Gets the current breaks settings.
        /// </summary>
        /// <returns>The breaks settings</returns>
        BreaksSettings GetSettings();

        /// <summary>
        /// Updates the time of no activity to start a break.
        /// </summary>
        /// <param name="timeOfNoActivityToStartBreak">The time span for no activity to start a break</param>
        void UpdateTimeOfNoActivityToStartBreak(TimeSpan timeOfNoActivityToStartBreak);

        /// <summary>
        /// Persists the breaks settings.
        /// </summary>
        void PersistSettings();
    }
}
