using MediatR;
using TrackYourDay.Core.Activities;

namespace TrackYourDay.Core.Tasks
{
    public class BreakTracker
    {
        private readonly TimeSpan timeOfNoActivityToStartBreak;
        private readonly bool isTrackingEnabled;

        private readonly IPublisher publisher;
        private readonly IClock clock;
        private Queue<ActivityEvent> eventsToProcess = new Queue<ActivityEvent>();
        private List<EndedBreak> endedBreaks = new List<EndedBreak>();
        private StartedBreak? currentBreak;
        private DateTime lastTimeOfActivity;

        public BreakTracker(IPublisher publisher, IClock clock, bool isTrackingEnabled)
        {
            this.publisher = publisher;
            this.isTrackingEnabled = isTrackingEnabled;
            this.timeOfNoActivityToStartBreak = TimeSpan.FromMinutes(5);
            this.clock = clock;
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
                    if (processedEvent.Activity is SystemLocked)
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
                    if (processedEvent.Activity is not SystemLocked)
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
