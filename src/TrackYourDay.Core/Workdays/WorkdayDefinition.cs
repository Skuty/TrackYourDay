namespace TrackYourDay.Core.Workdays
{
    /// <param name="WorkdayDuration">This time includes Time of Active working and Time of Breaks</param>
    /// <param name="AllowedBreakDuration">Time for breaks</param>
    public record class WorkdayDefinition(TimeSpan WorkdayDuration, TimeSpan AllowedBreakDuration)
    {
        /// <summary>List of Break Definitions with their allowed time and description</summary>
        public IReadOnlyCollection<BreakDefinition> BreakDefinitions { get; init; } = new List<BreakDefinition>();

        // TODO: Add missing tests that ensures that AllowedBreakDuration is equal to sum of breakDefinitions
        private WorkdayDefinition(TimeSpan workdayDuration, IList<BreakDefinition> breakDefinitions)
            : this(workdayDuration, new TimeSpan(breakDefinitions.Sum(b => b.Duration.Ticks))) 
        {
            this.BreakDefinitions = breakDefinitions.AsReadOnly();
        }

        public static WorkdayDefinition CreateDefaultDefinition()
        {
            return new WorkdayDefinition(TimeSpan.FromHours(11), TimeSpan.FromMinutes(50));
        }

        public static WorkdayDefinition CreateSampleCompanyDefinition()
        {
            var breaksDefinitions = new List<BreakDefinition>
            {
                new BreakDefinition(TimeSpan.FromMinutes(15), "law based Dinner Break"),
                new BreakDefinition(TimeSpan.FromMinutes(15), "Company additional Dinner Break"),
                new BreakDefinition(TimeSpan.FromMinutes(35), "Law based Offscreen Break")
            };

            return new WorkdayDefinition(TimeSpan.FromHours(10), breaksDefinitions);
        }
    }
}