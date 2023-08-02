using MediatR;
using System.Collections.Immutable;
using TrackYourDay.Core.Activities.Notifications;

namespace TrackYourDay.Core.Activities
{
    public class ActivityTracker
    {
        private readonly IClock clock;
        private readonly IPublisher publisher;
        private readonly IStartedActivityRecognizingStrategy startedActivityRecognizingStrategy;
        private readonly IInstantActivityRecognizingStrategy instantActivityRecognizingStrategy;
        private StartedActivity currentStartedActivity;
        private InstantActivity lastInstantActivity;
        private readonly List<EndedActivity> endedActivities;
        private readonly List<InstantActivity> instantActivities;

        public ActivityTracker(
            IClock clock,
            IPublisher publisher,
            IStartedActivityRecognizingStrategy startedActivityRecognizingStrategy,
            IInstantActivityRecognizingStrategy instantActivityRecognizingStrategy)
        {
            this.clock = clock;
            this.publisher = publisher;
            this.startedActivityRecognizingStrategy = startedActivityRecognizingStrategy;
            this.instantActivityRecognizingStrategy = instantActivityRecognizingStrategy;
            endedActivities = new List<EndedActivity>();
            instantActivities = new List<InstantActivity>();
        }

        internal void RecognizeEvents()
        {
            ActivityType recognizedActivityType = startedActivityRecognizingStrategy.RecognizeActivity();

            if (currentStartedActivity is null)
            {
                currentStartedActivity = ActivityFactory.StartedActivity(this.clock.Now, recognizedActivityType);
                publisher.Publish(new PeriodicActivityStartedNotification(Guid.NewGuid(), currentStartedActivity));
                return;
            }

            if (currentStartedActivity.ActivityType != recognizedActivityType)
            {
                var endedActivity = currentStartedActivity.End(this.clock.Now);
                endedActivities.Add(endedActivity);
                currentStartedActivity = ActivityFactory.StartedActivity(endedActivity.EndDate, recognizedActivityType);
                publisher.Publish(new PeriodicActivityEndedNotification(Guid.NewGuid(), endedActivity));
                publisher.Publish(new PeriodicActivityStartedNotification(Guid.NewGuid(), currentStartedActivity));
            };
        }

        internal StartedActivity? GetCurrentActivity()
        {
            return this.currentStartedActivity;
        }

        internal IReadOnlyCollection<EndedActivity> GetEndedActivities()
        {
            return this.endedActivities.ToImmutableArray();
        }
    }
}