using TrackYourDay.Core.SystemTrackers.SystemStates;
namespace TrackYourDay.Core.SystemTrackers
{
    public record class InstantActivity(Guid Guid, DateTime OccuranceDate, SystemState SystemState)
    {
        public TimeSpan GetDuration()
        {
            return TimeSpan.Zero;
        }
    }
}