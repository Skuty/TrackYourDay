using TrackYourDay.Core.Activities.SystemStates;
namespace TrackYourDay.Core.Activities
{
    public record class InstantActivity(DateTime OccuranceDate, SystemState ActivityType)
    {
        public TimeSpan GetDuration()
        {
            return TimeSpan.Zero;
        }
    }
}