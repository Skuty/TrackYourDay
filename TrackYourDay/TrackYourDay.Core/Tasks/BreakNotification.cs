using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrackYourDay.Core.Tasks
{
    public class BreakEndedNotifcation : INotification
    {
        public BreakEndedNotifcation(Guid notificationId, EndedBreak endedBreak)
        {
            this.NotificationId = notificationId;
            this.EndedBreak = endedBreak;
        }

        public Guid NotificationId { get; }

        public EndedBreak EndedBreak { get; }
    }

    public class BreakStartedNotifcation : INotification
    {
        public BreakStartedNotifcation(Guid notificationId, StartedBreak startedBreak)
        {
            this.NotificationId = notificationId;
            this.StartedBreak = startedBreak;
        }

        public Guid NotificationId { get; }

        public StartedBreak StartedBreak { get; }
    }
}
