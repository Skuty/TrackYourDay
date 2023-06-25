using MediatR;
using System.Collections.Immutable;

namespace TrackYourDay.Core.Activities
{
    public class ActivityEventTracker
    {
        private readonly IPublisher publisher;
        private readonly IActivityRecognizingStrategy activityRecognizingStrategy;
        private List<ActivityEvent> events;

        public ActivityEventTracker(IPublisher publisher, IActivityRecognizingStrategy activityRecognizingStrategy)
        {
            this.publisher = publisher;
            this.activityRecognizingStrategy = activityRecognizingStrategy;
            this.events = new List<ActivityEvent>();
        }

        public void RecognizeEvents()
        {
            Activity currentActivity = this.activityRecognizingStrategy.RecognizeActivity();
            if (currentActivity is null)
            {
                return;
            }

            if (events.Count == 0)
            {
                var newEvent = ActivityEvent.CreateEvent(DateTime.Now, currentActivity, "Starting Event");
                events.Add(newEvent);
                publisher.Publish(new ActivityEventRecognizedNotification(Guid.NewGuid(), newEvent));
                return;
            }

            ActivityEvent lastEvent = events.Last();
            if (currentActivity != lastEvent.Activity)
            {
                var newEvent = ActivityEvent.CreateEvent(DateTime.Now, currentActivity, "Event based on new activity");
                publisher.Publish(new ActivityEventRecognizedNotification(Guid.NewGuid(), newEvent));
                events.Add(newEvent);
            }
        }

        public ImmutableList<ActivityEvent> GetRegisteredEvents()
        {
            return events.ToImmutableList();
        }
    }
}