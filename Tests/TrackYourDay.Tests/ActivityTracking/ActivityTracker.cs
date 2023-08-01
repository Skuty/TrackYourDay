using MediatR;
using TrackYourDay.Core.Old.Activities.Notifications;
using TrackYourDay.Tests.Activities;

namespace TrackYourDay.Tests.ActivityTracking
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
            this.endedActivities = new List<EndedActivity>();
            this.instantActivities = new List<InstantActivity>();
        }

        internal void RecognizeEvents()
        {
            ActivityType recognizedActivityType = this.startedActivityRecognizingStrategy.RecognizeActivity();

            if (this.currentStartedActivity is null)
            {
                this.currentStartedActivity = ActivityFactory.StartedActivity(DateTime.Now, recognizedActivityType);
                this.publisher.Publish(new PeriodicActivityStartedNotification(Guid.NewGuid(), this.currentStartedActivity));
                return;
            }

            if (this.currentStartedActivity.ActivityType != recognizedActivityType)
            {
                var endedActivity = this.currentStartedActivity.End(DateTime.Now);
                this.endedActivities.Add(endedActivity);
                this.currentStartedActivity = ActivityFactory.StartedActivity(endedActivity.EndDate, recognizedActivityType);
                this.publisher.Publish(new PeriodicActivityEndedNotification(Guid.NewGuid(), endedActivity));
                this.publisher.Publish(new PeriodicActivityStartedNotification(Guid.NewGuid(), this.currentStartedActivity));
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