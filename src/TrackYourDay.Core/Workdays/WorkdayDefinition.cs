namespace TrackYourDay.Core.Workdays
{
    /// <param name="WorkdayDuration">This time includes Time of Active working and Time of Breaks</param>
    /// <param name="AllowedBreakDuration">Screen: 7x5 minutes = 35 minutes, Meal: 15 minutes</param>
    public record class WorkdayDefinition(TimeSpan WorkdayDuration, TimeSpan AllowedBreakDuration)
    {
        public static WorkdayDefinition CreateDefaultDefinition()
        {
            return new WorkdayDefinition(TimeSpan.FromHours(8), TimeSpan.FromMinutes(50));
        }

        public static WorkdayDefinition CreateSampleCompanyDefinition()
        {
            var lawBasedDinnerBreak = TimeSpan.FromMinutes(15);
            var companyAdditionalDinnerBreak = TimeSpan.FromMinutes(15);
            var offscreenLawBasedBreak = TimeSpan.FromMinutes(35);


            var workdayDuration = TimeSpan.FromHours(8);
            var allowedBreakDuration = TimeSpan.FromMinutes(50);

            return new WorkdayDefinition();
        }
    }
}
