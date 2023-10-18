namespace TrackYourDay.Core
{
    public static class Config
    {
        public static TimeSpan TimeOfNoActivityToStartBreak => TimeSpan.FromMinutes(5);

        public static TimeSpan FrequencyOfActivityDiscovering => TimeSpan.FromSeconds(5);

        public static TimeSpan WorkDayDuration => TimeSpan.FromHours(8);

        public static TimeSpan AllowedBreakDuration => TimeSpan.FromMinutes(90);
    }
}
