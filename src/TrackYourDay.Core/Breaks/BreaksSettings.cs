namespace TrackYourDay.Core.Breaks
{
    public class BreaksSettings
    {
        private readonly TimeSpan timeOfNoActivityToStartBreak;

        public BreaksSettings(TimeSpan timeOfNoActivityToStartBreak)
        {
            this.timeOfNoActivityToStartBreak = timeOfNoActivityToStartBreak;
        }

        public static BreaksSettings CreateDefaultSettings()
        {
            return new BreaksSettings(TimeSpan.FromMinutes(5));
        }


        public TimeSpan TimeOfNoActivityToStartBreak => this.timeOfNoActivityToStartBreak;
    }
}