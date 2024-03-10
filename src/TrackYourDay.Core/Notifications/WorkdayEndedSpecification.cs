using TrackYourDay.Core.Workdays;

/// <summary>
/// Draft of Notification
/// </summary>
/// <TODO>
/// Handle persisting different configuration for different notifications (conditions, etc)
/// </TODO>
namespace TrackYourDay.Core.Notifications
{
    public sealed class WorkdayEndedSpecification : NotificationExecuteSpecification
    {
        private readonly Workday workday;

        public WorkdayEndedSpecification(Workday workday)
        {
            this.workday = workday;
        }

        public override bool IsSatisfied()
        {
            return workday.TimeLeftToWorkActively < TimeSpan.FromMinutes(10);
        }
    }
}
