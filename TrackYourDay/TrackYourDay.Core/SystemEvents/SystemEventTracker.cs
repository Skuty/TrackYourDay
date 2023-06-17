using MediatR;
using System.Collections.Immutable;
using TrackYourDay.Core.Activities;

namespace TrackYourDay.Core.Events
{
    public class SystemEventTracker
    {
        private readonly IPublisher publisher;
        private readonly IActivityRecognizingStrategy activityRecognizingStrategy;
        private List<SystemEvent> Events;

        public SystemEventTracker(IPublisher publisher, IActivityRecognizingStrategy activityRecognizingStrategy)
        {
            this.publisher = publisher;
            this.activityRecognizingStrategy = activityRecognizingStrategy;
            Events = new List<SystemEvent>();
        }

        public void RecognizeEvents()
        {
            Activity currentActivity = activityRecognizingStrategy.RecognizeActivity();
            if (currentActivity is null)
            {
                return;
            }

            if (this.Events.Count == 0)
            {
                var newEvent = SystemEvent.CreateEvent(DateTime.Now, currentActivity, "Starting Event");
                this.Events.Add(newEvent);
                this.publisher.Publish(new SystemEventRecognizedNotification(Guid.NewGuid(), newEvent));
                return;
            }

            SystemEvent lastEvent = Events.Last();
            if (currentActivity != lastEvent.Activity)
            {
                var newEvent = SystemEvent.CreateEvent(DateTime.Now, currentActivity, "Event based on new activity");
                this.publisher.Publish(new SystemEventRecognizedNotification(Guid.NewGuid(), newEvent));
                this.Events.Add(newEvent);
            }
        }

        public ImmutableList<SystemEvent> GetRegisteredEvents()
        {
            return this.Events.ToImmutableList();
        }
    }
}