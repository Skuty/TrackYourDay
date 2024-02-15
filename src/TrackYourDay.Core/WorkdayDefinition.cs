namespace TrackYourDay.Core
{
    public class WorkdayDefinition
    {
        private readonly TimeSpan workdayDuration;
        private readonly TimeSpan allowedBreakDuration;

        public WorkdayDefinition(TimeSpan timeSpan1, TimeSpan timeSpan2)
        {
            this.workdayDuration = timeSpan1;
            this.allowedBreakDuration = timeSpan2;
        }

        /// <summary>
        /// This time includes Time of Active working and Time of Breaks
        /// </summary>
        public TimeSpan WorkdayDuration => workdayDuration;

        /// <summary>
        /// Screen: 7x5 minutes = 35 minutes
        /// Meal: 15 minutes
        /// </summary>
        public TimeSpan AllowedBreakDuration => allowedBreakDuration;

        public WorkdayDefinition CreateDefaultDefinition()
        {
            return new WorkdayDefinition(TimeSpan.FromHours(8), TimeSpan.FromMinutes(50));
        }
    }
}
