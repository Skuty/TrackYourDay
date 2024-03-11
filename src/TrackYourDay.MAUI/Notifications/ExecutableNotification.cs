/// <summary>
/// Draft of Notification
/// </summary>
/// <TODO>
/// Handle persisting different configuration for different notifications (conditions, etc)
/// </TODO>
namespace TrackYourDay.MAUI.Notifications
{
    public abstract class ExecutableNotification
    {
        public Guid Guid { get; private set; }

        public string Name { get; protected set; }

        public bool IsEnabled { get; private set; }

        protected ExecutableNotification()
        {
        }

        public ExecutableNotification(Guid guid, string name, bool isEnabled)
        {
            Guid = guid;
            Name = name ?? guid.ToString();
            IsEnabled = isEnabled;
        }

        public virtual bool ShouldBeExecuted()
        {
            return IsEnabled;
        }

        public virtual void Execute()
        {
            IsEnabled = false;
        }
    }
}
