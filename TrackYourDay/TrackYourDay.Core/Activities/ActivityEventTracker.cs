using MediatR;
using System.Collections.Immutable;

namespace TrackYourDay.Core.Activities
{
    public class ActivityEventTracker
    {
        private readonly IPublisher publisher;
        private readonly IActivityRecognizingStrategy activityRecognizingStrategy;
        private List<ActivityEvent> Events;

        public ActivityEventTracker(IPublisher publisher, IActivityRecognizingStrategy activityRecognizingStrategy)
        {
            this.publisher = publisher;
            this.activityRecognizingStrategy = activityRecognizingStrategy;
            Events = new List<ActivityEvent>();
        }

        public void RecognizeEvents()
        {
            Activity currentActivity = activityRecognizingStrategy.RecognizeActivity();
            if (currentActivity is null)
            {
                return;
            }

            if (Events.Count == 0)
            {
                var newEvent = ActivityEvent.CreateEvent(DateTime.Now, currentActivity, "Starting Event");
                Events.Add(newEvent);
                publisher.Publish(new ActivityEventRecognizedNotification(Guid.NewGuid(), newEvent));
                return;
            }

            ActivityEvent lastEvent = Events.Last();
            if (currentActivity != lastEvent.Activity)
            {
                var newEvent = ActivityEvent.CreateEvent(DateTime.Now, currentActivity, "Event based on new activity");
                publisher.Publish(new ActivityEventRecognizedNotification(Guid.NewGuid(), newEvent));
                Events.Add(newEvent);
            }
        }

        public ImmutableList<ActivityEvent> GetRegisteredEvents()
        {
            return Events.ToImmutableList();
        }
    }
}