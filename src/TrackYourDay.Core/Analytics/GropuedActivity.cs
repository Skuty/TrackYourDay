using TrackYourDay.Core.Activities;

namespace TrackYourDay.Core.Analytics
{
    public class GropuedActivity
    {
        private List<Guid> processedActivities { get; set; }

        public DateOnly Date { get; }

        public string ActivityDescription { get; }

        public TimeSpan Duration { get; private set; }

        public void Include(EndedActivity activity) 
        {
            if (!this.processedActivities.Contains(null))
            {
                this.Duration += activity.GetDuration();
                this.processedActivities.Add(null);
            }
        }
    }
}
