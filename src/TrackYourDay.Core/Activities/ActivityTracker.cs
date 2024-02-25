using MediatR;
using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
using TrackYourDay.Core.Activities.ActivityRecognizing;
using TrackYourDay.Core.Activities.Events;
using TrackYourDay.Core.Activities.SystemStates;
using TrackYourDay.Core.Breaks;

namespace TrackYourDay.Core.Activities
{
    public class ActivityTracker
    {
        private readonly ILogger<ActivityTracker> logger;

        private readonly IClock clock;
        private readonly IPublisher publisher;
        private readonly ISystemStateRecognizingStrategy systemStateRecognizingStrategy;
        private readonly ISystemStateRecognizingStrategy mousePositionRecognizingStrategy;
        private StartedActivity currentStartedActivity;
        private InstantActivity lastInstantActivity;
        private readonly List<EndedActivity> endedActivities;
        private readonly List<InstantActivity> instantActivities;

        public ActivityTracker(
            IClock clock,
            IPublisher publisher,
            ISystemStateRecognizingStrategy startedActivityRecognizingStrategy,
            ISystemStateRecognizingStrategy mousePositionRecognizingStrategy,
            ILogger<ActivityTracker> logger)
        {
            this.logger = logger;
            this.clock = clock;
            this.publisher = publisher;
            this.systemStateRecognizingStrategy = startedActivityRecognizingStrategy;
            this.mousePositionRecognizingStrategy = mousePositionRecognizingStrategy;
            this.endedActivities = new List<EndedActivity>();
            this.instantActivities = new List<InstantActivity>();
            this.currentStartedActivity = ActivityFactory.StartedActivity(
                this.clock.Now, SystemStateFactory.ApplicationStartedEvent("Track Your Day"));
        }

        public void RecognizeActivity()
        {
            SystemState recognizedSystemState = this.systemStateRecognizingStrategy.RecognizeActivity();

            if (this.currentStartedActivity is null)
            {
                this.currentStartedActivity = ActivityFactory.StartedActivity(this.clock.Now, recognizedSystemState);
                this.publisher.Publish(new PeriodicActivityStartedEvent(Guid.NewGuid(), this.currentStartedActivity));
                return;
            }

            if (this.currentStartedActivity.SystemState != recognizedSystemState)
            {
                var endedActivity = this.currentStartedActivity.End(this.clock.Now);
                this.endedActivities.Add(endedActivity);
                this.currentStartedActivity = ActivityFactory.StartedActivity(endedActivity.EndDate, recognizedSystemState);
                this.publisher.Publish(new PeriodicActivityEndedEvent(Guid.NewGuid(), endedActivity));
                this.publisher.Publish(new PeriodicActivityStartedEvent(Guid.NewGuid(), this.currentStartedActivity));
            };

            SystemState recognizedMousePosition = this.mousePositionRecognizingStrategy.RecognizeActivity();
            if (this.lastInstantActivity is null || this.lastInstantActivity.SystemState != recognizedMousePosition)
            {
                this.lastInstantActivity = ActivityFactory.MouseMovedActivity(this.clock.Now, recognizedMousePosition);
                this.publisher.Publish(new InstantActivityOccuredEvent(Guid.NewGuid(), this.lastInstantActivity));
                this.instantActivities.Add(this.lastInstantActivity);
            }
        }

        public StartedActivity GetCurrentActivity()
        {
            return this.currentStartedActivity;
        }

        public IReadOnlyCollection<EndedActivity> GetEndedActivities()
        {
            return this.endedActivities.ToImmutableArray();
        }

        public IReadOnlyCollection<InstantActivity> GetInstantActivities()
        {
            return this.instantActivities.ToImmutableArray();
        }
    }
}