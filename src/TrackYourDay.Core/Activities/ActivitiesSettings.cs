namespace TrackYourDay.Core.Activities
{
    public record class ActivitiesSettings(TimeSpan FrequencyOfActivityDiscovering)
    {
        public static ActivitiesSettings CreateDefaultSettings()
        {
            return new ActivitiesSettings(TimeSpan.FromSeconds(5));
        }
    }
}
