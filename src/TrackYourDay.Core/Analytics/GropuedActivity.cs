using TrackYourDay.Core.Activities;
using TrackYourDay.Core.Breaks;

namespace TrackYourDay.Core.Analytics
{
    public class GropuedActivity
    {
        // TODO probably have to be expanded to IncludedActivity with start / end date time
        private List<Guid> processedActivities;
        private List<Guid> processedBreaks;

        public DateOnly Date { get; }

        public string ActivityDescription { get; }

        public TimeSpan Duration { get; private set; }

        public static GropuedActivity CreateForDate(DateOnly date)
        {
            return new GropuedActivity(date);
        }

        public GropuedActivity(DateOnly date)
        {
            this.processedActivities = new List<Guid>();
            this.processedBreaks = new List<Guid>();
            this.Date = date;
            this.Duration = TimeSpan.Zero;
        }

        internal void Include(EndedActivity activityToInclude) 
        {
            if (!this.processedActivities.Contains(activityToInclude.Guid))
            {
                this.Duration += activityToInclude.GetDuration();
                this.processedActivities.Add(activityToInclude.Guid); 
            }
        }

        internal void ReduceBy(EndedBreak breakToReduce)
        {
            if (!this.processedActivities.Contains(breakToReduce..Guid))
            {
                this.Duration -= breakToReduce.BreakDuration;
                this.processedActivities.Add(breakToReduce.Guid);
            }
        }
    }
}
