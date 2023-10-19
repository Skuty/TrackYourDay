namespace TrackYourDay.Core
{
    public static class Config
    {
        public static TimeSpan TimeOfNoActivityToStartBreak => TimeSpan.FromMinutes(5);

        public static TimeSpan FrequencyOfActivityDiscovering => TimeSpan.FromSeconds(5);

        public static TimeSpan WorkDayDuration => TimeSpan.FromHours(8);

        /// <summary>
        /// Screen: 7x5 minutes = 35 minutes
        /// Meal: 15 minutes
        /// </summary>
        public static TimeSpan AllowedBreakDuration => TimeSpan.FromMinutes(50);
    }
}
