using TrackYourDay.Core.Activities;

namespace TrackYourDay.Core.Analytics
{
    internal record class IncludedPeriodicActivity(Guid OriginActivityGuid, DateTime StartDate, DateTime EndDate)
    {

        public static IncludedPeriodicActivity CreateFromEndedActivity(EndedActivity endedActivity)
        {
            return new IncludedPeriodicActivity(endedActivity.Guid, endedActivity.StartDate, endedActivity.EndDate);
        }
    }
}
