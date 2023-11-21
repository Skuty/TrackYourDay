using TrackYourDay.Core.Activities.SystemStates;
namespace TrackYourDay.Core.Activities
{
    public record class InstantActivity(DateTime OccuranceDate, SystemState SystemState)
    {
        public TimeSpan GetDuration()
        {
            return TimeSpan.Zero;
        }
    }
}