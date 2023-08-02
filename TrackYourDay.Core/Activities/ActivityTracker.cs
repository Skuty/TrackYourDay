using MediatR;
using TrackYourDay.Core.Activities.Notifications;

namespace TrackYourDay.Core.Activities
{
    public class ActivityTracker
    {
        private readonly IPublisher publisher;
        private readonly IStartedActivityRecognizingStrategy startedActivityRecognizingStrategy;
        private readonly IInstantActivityRecognizingStrategy instantActivityRecognizingStrategy;
        private StartedActivity currentStartedActivity;
        private InstantActivity lastInstantActivity;
        private readonly List<EndedActivity> endedActivities;
        private readonly List<InstantActivity> instantActivities;

        public ActivityTracker(
            IPublisher publisher,
            IStartedActivityRecognizingStrategy startedActivityRecognizingStrategy,
            IInstantActivityRecognizingStrategy instantActivityRecognizingStrategy)
        {
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
                currentStartedActivity = ActivityFactory.StartedActivity(DateTime.Now, recognizedActivityType);
                publisher.Publish(new PeriodicActivityStartedNotification(Guid.NewGuid(), currentStartedActivity));
                return;
            }

            if (currentStartedActivity.ActivityType != recognizedActivityType)
            {
                var endedActivity = currentStartedActivity.End(DateTime.Now);
                endedActivities.Add(endedActivity);
                currentStartedActivity = ActivityFactory.StartedActivity(endedActivity.EndDate, recognizedActivityType);
                publisher.Publish(new PeriodicActivityEndedNotification(Guid.NewGuid(), endedActivity));
                publisher.Publish(new PeriodicActivityStartedNotification(Guid.NewGuid(), currentStartedActivity));
            };
        }

        internal StartedActivity? GetCurrentActivity()
        {
            throw new NotImplementedException();
        }

        internal List<EndedActivity> GetEndedActivities()
        {
            throw new NotImplementedException();
        }
    }
}