using MediatR;
using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
using TrackYourDay.Core.Activities.ActivityRecognizing;
using TrackYourDay.Core.Activities.Notifications;
using TrackYourDay.Core.Activities.SystemStates;

namespace TrackYourDay.Core.Activities
{
    public class ActivityTracker
    {
        private readonly ILogger<ActivityTracker> logger;

        private readonly IClock clock;
        private readonly IPublisher publisher;
        private readonly ISystemStateRecognizingStrategy startedActivityRecognizingStrategy;
        private readonly IInstantActivityRecognizingStrategy instantActivityRecognizingStrategy;
        private StartedActivity currentStartedActivity;
        private InstantActivity lastInstantActivity;
        private readonly List<EndedActivity> endedActivities;
        private readonly List<InstantActivity> instantActivities;

        public ActivityTracker(
            IClock clock,
            IPublisher publisher,
            ISystemStateRecognizingStrategy startedActivityRecognizingStrategy,
            IInstantActivityRecognizingStrategy instantActivityRecognizingStrategy,
            ILogger<ActivityTracker> logger)
        {
            this.logger = logger;
            this.clock = clock;
            this.publisher = publisher;
            this.startedActivityRecognizingStrategy = startedActivityRecognizingStrategy;
            this.instantActivityRecognizingStrategy = instantActivityRecognizingStrategy;
            this.endedActivities = new List<EndedActivity>();
            this.instantActivities = new List<InstantActivity>();
            this.currentStartedActivity = ActivityFactory.StartedActivity(
                this.clock.Now, SystemStateFactory.ApplicationStartedEvent("Track Your Day"));
        }

        public void RecognizeActivity()
        {
            SystemState recognizedActivityType = this.startedActivityRecognizingStrategy.RecognizeActivity();

            if (this.currentStartedActivity is null)
            {
                this.currentStartedActivity = ActivityFactory.StartedActivity(this.clock.Now, recognizedActivityType);
                this.publisher.Publish(new PeriodicActivityStartedNotification(Guid.NewGuid(), this.currentStartedActivity));
                return;
            }

            if (this.currentStartedActivity.ActivityType != recognizedActivityType)
            {
                var endedActivity = this.currentStartedActivity.End(this.clock.Now);
                this.endedActivities.Add(endedActivity);
                this.currentStartedActivity = ActivityFactory.StartedActivity(endedActivity.EndDate, recognizedActivityType);
                this.publisher.Publish(new PeriodicActivityEndedNotification(Guid.NewGuid(), endedActivity));
                this.publisher.Publish(new PeriodicActivityStartedNotification(Guid.NewGuid(), this.currentStartedActivity));
            };
        }

        public StartedActivity GetCurrentActivity()
        {
            return this.currentStartedActivity;
        }

        public IReadOnlyCollection<EndedActivity> GetEndedActivities()
        {
            return this.endedActivities.ToImmutableArray();
        }
    }
}