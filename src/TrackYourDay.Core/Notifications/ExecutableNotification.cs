/// <summary>
/// Draft of Notification
/// </summary>
/// <TODO>
/// Handle persisting different configuration for different notifications (conditions, etc)
/// </TODO>
namespace TrackYourDay.Core.Notifications
{
    public class ExecutableNotification
    {
        private readonly NotificationExecuteSpecification notificationExecuteSpecification;
        private readonly NotificationAction notificationAction;

        public Guid Guid { get; }

        public string Name { get; }

        public bool IsEnabled { get; private set; }

        public ExecutableNotification()
        {
        }

        public ExecutableNotification(
            NotificationExecuteSpecification notificationExecuteSpecification,
            NotificationAction notificationAction) 
        {
            this.Guid = Guid.NewGuid();
            this.Name = this.Guid.ToString();
            this.notificationExecuteSpecification = notificationExecuteSpecification;
            this.notificationAction = notificationAction;
        }

        public ExecutableNotification(
            string name, 
            NotificationExecuteSpecification notificationExecuteSpecification,
            NotificationAction notificationAction)
        {
            this.Guid = Guid.NewGuid();
            this.Name = name;
            this.notificationExecuteSpecification = notificationExecuteSpecification;
            this.notificationAction = notificationAction;
        }

        public virtual bool ShouldBeExecuted()
        {
            return this.notificationExecuteSpecification.IsSatisfied();
        }

        public virtual void Execute()
        {
            this.notificationAction.Execute();
        }
    }
}
