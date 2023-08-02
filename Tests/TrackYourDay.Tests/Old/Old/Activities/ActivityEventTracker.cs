﻿using MediatR;
using System.Collections.Immutable;
using TrackYourDay.Core.Old.Activities.Notifications;
using TrackYourDay.Core.Old.Activities.RecognizingStrategies;

namespace TrackYourDay.Core.Old.Activities
{
    public class ActivityEventTracker
    {
        private readonly IPublisher publisher;
        private readonly IActivityRecognizingStrategy activityRecognizingStrategy;
        private List<ActivityEvent> activityEvents;

        public ActivityEventTracker(IPublisher publisher, IActivityRecognizingStrategy activityRecognizingStrategy)
        {
            this.publisher = publisher;
            this.activityRecognizingStrategy = activityRecognizingStrategy;
            activityEvents = new List<ActivityEvent>();
        }

        public void RecognizeEvents()
        {
            Activity currentActivity = activityRecognizingStrategy.RecognizeActivity();
            if (currentActivity is null)
            {
                return;
            }

            if (activityEvents.Count == 0)
            {
                var newEvent = ActivityEvent.CreateEvent(DateTime.Now, currentActivity);
                activityEvents.Add(newEvent);
                publisher.Publish(new ActivityEventRecognizedNotification(Guid.NewGuid(), newEvent));
                return;
            }

            ActivityEvent lastEvent = activityEvents.Last();
            if (currentActivity != lastEvent.Activity)
            {
                var newEvent = ActivityEvent.CreateEvent(DateTime.Now, currentActivity);
                activityEvents.Add(newEvent);
                publisher.Publish(new ActivityEventRecognizedNotification(Guid.NewGuid(), newEvent));
            }
        }

        public ImmutableList<ActivityEvent> GetRegisteredActivities()
        {
            return activityEvents.ToImmutableList();
        }
    }
}