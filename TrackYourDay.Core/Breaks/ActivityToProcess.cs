using TrackYourDay.Core.Activities;

namespace TrackYourDay.Core.Breaks
{
    public record class ActivityToProcess(DateTime ActivityDate, ActivityType ActivityType);
}
