namespace TrackYourDay.Core
{
    public static class Config
    {
        public static TimeSpan TimeOfNoActivityToStartBreak => TimeSpan.FromMinutes(1);

        public static TimeSpan FrequencyOfActivityDiscovering => TimeSpan.FromSeconds(5);

        /// <summary>
        /// This time includes Time of Active working and Time of Breaks
        /// </summary>
        public static TimeSpan WorkdayDuration => TimeSpan.FromHours(8);

        /// <summary>
        /// Screen: 7x5 minutes = 35 minutes
        /// Meal: 15 minutes
        /// </summary>
        public static TimeSpan AllowedBreakDuration => TimeSpan.FromMinutes(50);
    }
}
