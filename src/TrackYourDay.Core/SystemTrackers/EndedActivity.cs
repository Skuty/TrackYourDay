using TrackYourDay.Core.SystemTrackers.SystemStates;

namespace TrackYourDay.Core.SystemTrackers
{
    public record class EndedActivity(DateTime StartDate, DateTime EndDate, SystemState ActivityType)
    {
        public Guid Guid { get; init; } = Guid.NewGuid();

        public TimeSpan GetDuration()
        {
            return EndDate - StartDate;
        }

        public string GetDescription()
        {
            return ActivityType.ActivityDescription;
        }
    }
}