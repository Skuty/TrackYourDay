/// <summary>
/// Draft of Notification
/// </summary>
/// <TODO>
/// Handle persisting different configuration for different notifications (conditions, etc)
/// </TODO>
namespace TrackYourDay.Core.Notifications
{
    public sealed class ExecutedNotification
    {
        public ExecutedNotification(Guid id, DateTime executedAt)
        {
            Id = id;
            ExecutedAt = executedAt;
        }

        public Guid Id { get; }

        public DateTime ExecutedAt { get; }

        public static ExecutedNotification CreateFrom(ExecutableNotification scheduledNotification)
        {
            return new ExecutedNotification(scheduledNotification.Guid, DateTime.Now);
        }
    }
}
