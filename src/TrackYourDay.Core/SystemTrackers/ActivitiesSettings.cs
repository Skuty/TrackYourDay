namespace TrackYourDay.Core.SystemTrackers
{
    public record class ActivitiesSettings(TimeSpan FrequencyOfActivityDiscovering)
    {
        public static ActivitiesSettings CreateDefaultSettings()
        {
            return new ActivitiesSettings(TimeSpan.FromSeconds(5));
        }
    }
}
