namespace TrackYourDay.Core.Activities
{
    public class ActivitiesSettings
    {
        private readonly TimeSpan frequencyOfActivityDiscovering;

        public ActivitiesSettings(TimeSpan frequencyOfActivityDiscovering)
        {
            this.frequencyOfActivityDiscovering = frequencyOfActivityDiscovering;
        }

        public TimeSpan FrequencyOfActivityDiscovering => this.frequencyOfActivityDiscovering;

        public static ActivitiesSettings CreateDefaultSettings()
        {
            return new ActivitiesSettings(TimeSpan.FromSeconds(5));
        }
    }
}
