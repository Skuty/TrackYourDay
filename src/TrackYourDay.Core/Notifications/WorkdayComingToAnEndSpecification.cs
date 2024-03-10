using TrackYourDay.Core.Workdays;

/// <summary>
/// Draft of Notification
/// </summary>
/// <TODO>
/// Handle persisting different configuration for different notifications (conditions, etc)
/// </TODO>
namespace TrackYourDay.Core.Notifications
{
    public sealed class WorkdayComingToAnEndSpecification : NotificationExecuteSpecification
    {
        private readonly Workday workday;

        public WorkdayComingToAnEndSpecification(Workday workday)
        {
            this.workday = workday;
        }

        public override bool IsSatisfied()
        {
            return workday.TimeLeftToWorkActively < TimeSpan.FromMinutes(60);
        }
    }
}
