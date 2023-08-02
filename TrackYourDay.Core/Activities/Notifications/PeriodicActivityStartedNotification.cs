﻿using MediatR;

namespace TrackYourDay.Core.Activities.Notifications
{
    internal record class PeriodicActivityStartedNotification(Guid Guid, StartedActivity StartedActivity) : INotification;
}