namespace TrackYourDay.Core
{
    public class WorkdayDefinition
    {
        /// <summary>
        /// This time includes Time of Active working and Time of Breaks
        /// </summary>
        public TimeSpan WorkdayDuration => TimeSpan.FromHours(8);

        /// <summary>
        /// Screen: 7x5 minutes = 35 minutes
        /// Meal: 15 minutes
        /// </summary>
        public TimeSpan AllowedBreakDuration => TimeSpan.FromMinutes(50);
    }
}
