namespace TrackYourDay.Core.Activities
{
    public class ActivitiesSettings
    {
        private readonly TimeSpan frequencyOfActivityDiscovering;

        public ActivitiesSettings()
        {
            this.frequencyOfActivityDiscovering = TimeSpan.FromSeconds(5);
        }

        public ActivitiesSettings(TimeSpan frequencyOfActivityDiscovering)
        {
            this.frequencyOfActivityDiscovering = frequencyOfActivityDiscovering;
        }

        public TimeSpan FrequencyOfActivityDiscovering => this.frequencyOfActivityDiscovering;
    }
}
