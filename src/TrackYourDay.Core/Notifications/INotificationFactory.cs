namespace TrackYourDay.Core.Notifications
{
    public interface INotificationFactory
    {
        public ExecutableNotification GetNotificationByName(string name);

        public ExecutableNotification GetDefaultNotification();
    }
}
