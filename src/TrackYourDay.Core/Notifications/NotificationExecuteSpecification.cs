﻿/// <summary>
/// Draft of Notification
/// </summary>
/// <TODO>
/// Handle persisting different configuration for different notifications (conditions, etc)
/// </TODO>
namespace TrackYourDay.Core.Notifications
{
    public abstract class NotificationExecuteSpecification
    {
        public abstract bool IsSatisfied();
    }
}