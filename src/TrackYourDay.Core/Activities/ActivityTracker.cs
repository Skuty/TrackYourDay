using MediatR;
using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
using TrackYourDay.Core.Activities.ActivityRecognizing;
using TrackYourDay.Core.Activities.Events;
using TrackYourDay.Core.Activities.SystemStates;

namespace TrackYourDay.Core.Activities
{
    public class ActivityTracker
    {
        private readonly ILogger<ActivityTracker> logger;

        private readonly IClock clock;
        private readonly IPublisher publisher;
        private readonly ISystemStateRecognizingStrategy focusedWindowRecognizingStategy;
        private readonly ISystemStateRecognizingStrategy mousePositionRecognizingStrategy;
        private readonly ISystemStateRecognizingStrategy lastInputRecognizingStrategy;
        private StartedActivity currentStartedActivity;
        private InstantActivity recognizedMousePositionActivity;
        private InstantActivity lastInputActivity;
        private readonly List<EndedActivity> endedActivities;
        private readonly List<InstantActivity> instantActivities;

        public ActivityTracker(
            IClock clock,
            IPublisher publisher,
            ISystemStateRecognizingStrategy startedActivityRecognizingStrategy,
            ISystemStateRecognizingStrategy mousePositionRecognizingStrategy,
            ISystemStateRecognizingStrategy lastInputRecognizingStrategy,
            ILogger<ActivityTracker> logger)
        {
            this.logger = logger;
            this.clock = clock;
            this.publisher = publisher;
            this.focusedWindowRecognizingStategy = startedActivityRecognizingStrategy;
            this.mousePositionRecognizingStrategy = mousePositionRecognizingStrategy;
            this.lastInputRecognizingStrategy = lastInputRecognizingStrategy;
            this.endedActivities = new List<EndedActivity>();
            this.instantActivities = new List<InstantActivity>();
            this.currentStartedActivity = ActivityFactory.StartedActivity(
                this.clock.Now, SystemStateFactory.ApplicationStartedEvent("Track Your Day"));
        }

        public void RecognizeActivity()
        {
            SystemState recognizedFocusedWindow = this.focusedWindowRecognizingStategy.RecognizeActivity();

            if (this.currentStartedActivity is null)
            {
                this.currentStartedActivity = ActivityFactory.StartedActivity(this.clock.Now, recognizedFocusedWindow);
                this.publisher.Publish(new PeriodicActivityStartedEvent(Guid.NewGuid(), this.currentStartedActivity));
                return;
            }

            if (this.currentStartedActivity.SystemState != recognizedFocusedWindow)
            {
                var endedActivity = this.currentStartedActivity.End(this.clock.Now);
                this.endedActivities.Add(endedActivity);
                this.currentStartedActivity = ActivityFactory.StartedActivity(endedActivity.EndDate, recognizedFocusedWindow);
                this.publisher.Publish(new PeriodicActivityEndedEvent(Guid.NewGuid(), endedActivity));
                this.publisher.Publish(new PeriodicActivityStartedEvent(Guid.NewGuid(), this.currentStartedActivity));
            };

            // Remove in future as it is redundant due to LastInput recognition covering also mouse movement as input
            //SystemState newlyRecognizedMousePosition = this.mousePositionRecognizingStrategy.RecognizeActivity();
            //if (this.recognizedMousePositionActivity is null || this.recognizedMousePositionActivity.SystemState != newlyRecognizedMousePosition)
            //{
            //    this.recognizedMousePositionActivity = ActivityFactory.MouseMovedActivity(this.clock.Now, newlyRecognizedMousePosition);
            //    this.publisher.Publish(new InstantActivityOccuredEvent(Guid.NewGuid(), this.recognizedMousePositionActivity));
            //    this.instantActivities.Add(this.recognizedMousePositionActivity);
            //}

            SystemState newlyRecognizedLastInput = this.lastInputRecognizingStrategy.RecognizeActivity();
            if (this.lastInputActivity is null || this.lastInputActivity.SystemState != newlyRecognizedLastInput)
            {
                this.lastInputActivity = ActivityFactory.LastInputActivity(this.clock.Now, newlyRecognizedLastInput);
                this.publisher.Publish(new InstantActivityOccuredEvent(Guid.NewGuid(), this.lastInputActivity));
                this.instantActivities.Add(this.lastInputActivity);
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