namespace TrackYourDay.Core.ApplicationTrackers.Breaks
{
    public record class BreaksSettings(TimeSpan TimeOfNoActivityToStartBreak)
    {
        public static BreaksSettings CreateDefaultSettings()
        {
            return new BreaksSettings(TimeSpan.FromMinutes(5));
        }
    }
}