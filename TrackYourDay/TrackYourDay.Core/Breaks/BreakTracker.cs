using MediatR;
using TrackYourDay.Core.Activities;
using TrackYourDay.Core.Breaks.Notifications;

namespace TrackYourDay.Core.Breaks
{
    public class BreakTracker
    {
        private readonly TimeSpan timeOfNoActivityToStartBreak;
        private readonly bool isTrackingEnabled;

        private readonly IPublisher publisher;
        private readonly IClock clock;
        private Queue<ActivityEvent> eventsToProcess = new Queue<ActivityEvent>();
        public List<EndedBreak> endedBreaks = new List<EndedBreak>();
        private StartedBreak? currentBreak;
        private DateTime lastTimeOfActivity;

        public BreakTracker(IPublisher publisher, IClock clock, bool isTrackingEnabled)
        {
            this.publisher = publisher;
            this.isTrackingEnabled = isTrackingEnabled;
            this.timeOfNoActivityToStartBreak = TimeSpan.FromSeconds(5);
            this.clock = clock;
            this.lastTimeOfActivity = this.clock.Now;
        }

        public BreakTracker(IPublisher publisher, IClock clock)
        {
            this.publisher = publisher;
            this.isTrackingEnabled = true;
            this.timeOfNoActivityToStartBreak = TimeSpan.FromSeconds(5);
            this.clock = clock;
            this.lastTimeOfActivity = this.clock.Now;
        }


        // <summary> This constructor is used only for testing purposes. It should be marked as internal/private in future.<summary>
        public BreakTracker(StartedBreak startedBreak, IPublisher publisher, IClock clock, bool isTrackingEnabled) : this(publisher, clock, isTrackingEnabled)
        {
            this.currentBreak = startedBreak;
        }

        public void AddActivityEventToProcess(ActivityEvent systemEvent)
        {
            if (!isTrackingEnabled)
            {
                return;
            }

            if (systemEvent is null)
            {
                throw new ArgumentNullException(nameof(systemEvent));
            }

            this.eventsToProcess.Enqueue(systemEvent);
        }

        public void ProcessActivityEvents()
        {
            if (!isTrackingEnabled)
            {
                return;
            }

            while (eventsToProcess.Any())
            {
                var processedEvent = eventsToProcess.Dequeue();
                // Starting break;
                if (this.currentBreak is null)
                {
                    // Start Break If System Is Locked
                    if (processedEvent.Activity is SystemLockedActivity)
                    {
                        this.currentBreak = new StartedBreak(processedEvent.EventDate);
                        this.publisher.Publish(new BreakStartedNotifcation(Guid.NewGuid(), this.currentBreak));
                        this.lastTimeOfActivity = processedEvent.EventDate;
                        continue;
                    }

                    // Start Break if there was no Activity for specified amount of time between events
                    if (processedEvent.EventDate - this.lastTimeOfActivity > this.timeOfNoActivityToStartBreak)
                    {
                        this.currentBreak = new StartedBreak(processedEvent.EventDate);
                        this.publisher.Publish(new BreakStartedNotifcation(Guid.NewGuid(), this.currentBreak));
                        this.lastTimeOfActivity = processedEvent.EventDate;
                        continue;
                    }
                }

                // Ending break;
                if (this.currentBreak is not null)
                {
                    if (processedEvent.Activity is not SystemLockedActivity)
                    {
                        var endedBreak = this.currentBreak.EndBreak(processedEvent.EventDate);
                        this.endedBreaks.Add(endedBreak);
                        this.currentBreak = null;
                        this.publisher.Publish(new BreakEndedNotifcation(Guid.NewGuid(), endedBreak));
                        this.lastTimeOfActivity = processedEvent.EventDate;
                        continue;
                    }
                }
            }

            // Start Break if there was no Activity for specified amount of time between last event and now
            if (this.clock.Now - this.lastTimeOfActivity > this.timeOfNoActivityToStartBreak)
            {
                this.currentBreak = new StartedBreak(this.clock.Now);
                this.publisher.Publish(new BreakStartedNotifcation(Guid.NewGuid(), this.currentBreak));
                this.lastTimeOfActivity = this.currentBreak.BreakStartedOn;
            }
        }
    }
}
