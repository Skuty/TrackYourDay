using MediatR;
using TrackYourDay.Core.Activities;

namespace TrackYourDay.Core.Tasks
{
    public class BreakTracker
    {
        private readonly TimeSpan timeOfNoActivityToStartBreak;
        private readonly bool isTrackingEnabled;
        
        private readonly IPublisher publisher;
        private Queue<ActivityEvent> eventsToProcess = new Queue<ActivityEvent>();
        private List<EndedBreak> endedBreaks = new List<EndedBreak>();
        private StartedBreak? currentBreak;


        public BreakTracker(IPublisher publisher, bool isTrackingEnabled)
        {
            this.publisher = publisher;
            this.isTrackingEnabled = isTrackingEnabled;
            this.timeOfNoActivityToStartBreak = TimeSpan.FromMinutes(5);
        }

        public void AddEventToProcess(ActivityEvent systemEvent)
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

        public void ProcessEvents()
        {
            if (!isTrackingEnabled)
            {
                return;
            }

            while (eventsToProcess.Any())
            {
                var processedEvent = eventsToProcess.Dequeue();
                if (this.currentBreak is null)
                {
                    if (processedEvent.Activity is SystemLocked)
                    {
                        this.currentBreak = new StartedBreak(processedEvent.EventDate);
                        this.publisher.Publish(new BreakStartedNotifcation(Guid.NewGuid(), this.currentBreak));
                        continue;
                        ;
                    }
                }

                if (this.currentBreak is not null)
                {
                    if (processedEvent.Activity is not SystemLocked)
                    {
                        var endedBreak = this.currentBreak.EndBreak(processedEvent.EventDate);
                        this.endedBreaks.Add(endedBreak);
                        this.currentBreak = null;
                        this.publisher.Publish(new BreakEndedNotifcation(Guid.NewGuid(), endedBreak));
                        continue;
                    }
                }
            }
        }
    }
}
