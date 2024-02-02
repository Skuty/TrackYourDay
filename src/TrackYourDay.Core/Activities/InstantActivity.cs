using TrackYourDay.Core.Activities.SystemStates;
namespace TrackYourDay.Core.Activities
{
    public record class InstantActivity(Guid Guid, DateTime OccuranceDate, SystemState SystemState)
    {
        public TimeSpan GetDuration()
        {
            return TimeSpan.Zero;
        }
    }
}