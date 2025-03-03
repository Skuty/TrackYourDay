using TrackYourDay.Core.SystemTrackers.SystemStates;

namespace TrackYourDay.Core.ApplicationTrackers.Breaks
{
    public record class ActivityToProcess(DateTime ActivityDate, SystemState ActivityType, Guid ActivityGuid);
}
