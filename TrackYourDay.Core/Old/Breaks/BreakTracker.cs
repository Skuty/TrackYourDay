using MediatR;
using TrackYourDay.Core.Breaks.Notifications;
using TrackYourDay.Core.Old.Activities;

namespace TrackYourDay.Core.Old.Breaks
{
    public class BreakTracker
    {
        private readonly TimeSpan timeOfNoActivityToStartBreak;
        private readonly IPublisher publisher;
        private readonly IClock clock;
        private Queue<ActivityEvent> eventsToProcess = new Queue<ActivityEvent>();
        public List<EndedBreak> endedBreaks = new List<EndedBreak>();
        private StartedBreak? currentBreak;
        private DateTime lastTimeOfActivity;

        public BreakTracker(IPublisher publisher, IClock clock)
        {
            this.publisher = publisher;
            timeOfNoActivityToStartBreak = TimeSpan.FromSeconds(5);
            this.clock = clock;
            lastTimeOfActivity = this.clock.Now;
        }

        // <summary> This constructor is used only for testing purposes. It should be marked as internal/private in future.<summary>
        public BreakTracker(StartedBreak startedBreak, IPublisher publisher, IClock clock) : this(publisher, clock)
        {
            currentBreak = startedBreak;
        }

        public void AddActivityEventToProcess(ActivityEvent systemEvent)
        {
            if (systemEvent is null)
            {
                throw new ArgumentNullException(nameof(systemEvent));
            }

            eventsToProcess.Enqueue(systemEvent);
        }

        public void ProcessActivityEvents()
        {
            while (eventsToProcess.Any())
            {
                var processedEvent = eventsToProcess.Dequeue();
                // Starting break;
                if (currentBreak is null)
                {
                    // Start Break If System Is Locked
                    if (processedEvent.Activity is SystemLockedActivity)
                    {
                        currentBreak = new StartedBreak(processedEvent.EventDate);
                        publisher.Publish(new BreakStartedNotifcation(Guid.NewGuid(), currentBreak));
                        lastTimeOfActivity = processedEvent.EventDate;
                        continue;
                    }

                    // Start Break if there was no Activity for specified amount of time between events
                    if (processedEvent.EventDate - lastTimeOfActivity > timeOfNoActivityToStartBreak)
                    {
                        currentBreak = new StartedBreak(processedEvent.EventDate);
                        publisher.Publish(new BreakStartedNotifcation(Guid.NewGuid(), currentBreak));
                        lastTimeOfActivity = processedEvent.EventDate;
                        continue;
                    }
                }

                // Ending break;
                if (currentBreak is not null)
                {
                    if (processedEvent.Activity is not SystemLockedActivity)
                    {
                        var endedBreak = currentBreak.EndBreak(processedEvent.EventDate);
                        endedBreaks.Add(endedBreak);
                        currentBreak = null;
                        publisher.Publish(new BreakEndedNotifcation(Guid.NewGuid(), endedBreak));
                        lastTimeOfActivity = processedEvent.EventDate;
                        continue;
                    }
                }
            }

            // Start Break if there was no Activity for specified amount of time between last event and now
            if (clock.Now - lastTimeOfActivity > timeOfNoActivityToStartBreak)
            {
                currentBreak = new StartedBreak(clock.Now);
                publisher.Publish(new BreakStartedNotifcation(Guid.NewGuid(), currentBreak));
                lastTimeOfActivity = currentBreak.BreakStartedOn;
            }
        }
    }
}
