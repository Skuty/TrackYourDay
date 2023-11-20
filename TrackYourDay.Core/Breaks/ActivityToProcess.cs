using TrackYourDay.Core.Activities.SystemStates;

namespace TrackYourDay.Core.Breaks
{
    public record class ActivityToProcess(DateTime ActivityDate, SystemState ActivityType, Guid ActivityGuid);
}
