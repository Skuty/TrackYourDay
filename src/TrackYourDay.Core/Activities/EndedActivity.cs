using TrackYourDay.Core.Activities.SystemStates;

namespace TrackYourDay.Core.Activities
{
    public record class EndedActivity(DateTime StartDate, DateTime EndDate, SystemState ActivityType)
    {
        public Guid Guid { get; init; } = Guid.NewGuid();

        public TimeSpan GetDuration()
        {
            return this.EndDate - this.StartDate;
        }

        public string GetDescription()
        {
            return this.ActivityType.ActivityDescription;
        }
    }
}