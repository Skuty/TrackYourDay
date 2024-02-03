namespace TrackYourDay.Core.Breaks
{
    public class BreaksSettings
    {
        private TimeSpan timeOfNoActivityToStartBreak;

        public BreaksSettings()
        {
            this.timeOfNoActivityToStartBreak = TimeSpan.FromMinutes(5);
        }

        public BreaksSettings(TimeSpan timeOfNoActivityToStartBreak)
        {
            this.timeOfNoActivityToStartBreak = timeOfNoActivityToStartBreak;
        }

        public TimeSpan TimeOfNoActivityToStartBreak => this.timeOfNoActivityToStartBreak;
    }
}