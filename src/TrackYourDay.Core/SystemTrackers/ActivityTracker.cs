using MediatR;
using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
using TrackYourDay.Core.SystemTrackers.ActivityRecognizing;
using TrackYourDay.Core.SystemTrackers.Events;
using TrackYourDay.Core.SystemTrackers.SystemStates;

namespace TrackYourDay.Core.SystemTrackers
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
            focusedWindowRecognizingStategy = startedActivityRecognizingStrategy;
            this.mousePositionRecognizingStrategy = mousePositionRecognizingStrategy;
            this.lastInputRecognizingStrategy = lastInputRecognizingStrategy;
            endedActivities = new List<EndedActivity>();
            instantActivities = new List<InstantActivity>();
            currentStartedActivity = ActivityFactory.StartedActivity(
                this.clock.Now, SystemStateFactory.ApplicationStartedEvent("Track Your Day"));
        }

        public void RecognizeActivity()
        {
            SystemState recognizedFocusedWindow = focusedWindowRecognizingStategy.RecognizeActivity();

            if (currentStartedActivity is null)
            {
                currentStartedActivity = ActivityFactory.StartedActivity(clock.Now, recognizedFocusedWindow);
                publisher.Publish(new PeriodicActivityStartedEvent(Guid.NewGuid(), currentStartedActivity));
                return;
            }

            if (currentStartedActivity.SystemState != recognizedFocusedWindow)
            {
                var endedActivity = currentStartedActivity.End(clock.Now);
                endedActivities.Add(endedActivity);
                
                currentStartedActivity = ActivityFactory.StartedActivity(endedActivity.EndDate, recognizedFocusedWindow);
                publisher.Publish(new PeriodicActivityEndedEvent(Guid.NewGuid(), endedActivity));
                publisher.Publish(new PeriodicActivityStartedEvent(Guid.NewGuid(), currentStartedActivity));
            };

            // Remove in future as it is redundant due to LastInput recognition covering also mouse movement as input
            SystemState newlyRecognizedMousePosition = mousePositionRecognizingStrategy.RecognizeActivity();
            if (recognizedMousePositionActivity is null || recognizedMousePositionActivity.SystemState != newlyRecognizedMousePosition)
            {
                recognizedMousePositionActivity = ActivityFactory.MouseMovedActivity(clock.Now, newlyRecognizedMousePosition);
                publisher.Publish(new InstantActivityOccuredEvent(Guid.NewGuid(), recognizedMousePositionActivity));
                instantActivities.Add(recognizedMousePositionActivity);
            }

            //SystemState newlyRecognizedLastInput = this.lastInputRecognizingStrategy.RecognizeActivity();
            //if (this.lastInputActivity is null || this.lastInputActivity.SystemState != newlyRecognizedLastInput)
            //{
            //    this.lastInputActivity = ActivityFactory.LastInputActivity(this.clock.Now, newlyRecognizedLastInput);
            //    this.publisher.Publish(new InstantActivityOccuredEvent(Guid.NewGuid(), this.lastInputActivity));
            //    this.instantActivities.Add(this.lastInputActivity);
            //}
        }

        public StartedActivity GetCurrentActivity()
        {
            return currentStartedActivity;
        }

        public IReadOnlyCollection<EndedActivity> GetEndedActivities()
        {
            return endedActivities.ToImmutableArray();
        }

        public IReadOnlyCollection<InstantActivity> GetInstantActivities()
        {
            return instantActivities.ToImmutableArray();
        }
    }
}