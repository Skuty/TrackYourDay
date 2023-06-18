using MediatR;
using TrackYourDay.Core.Events;

namespace TrackYourDay.Core.Tasks
{
    public class BreakTracker
    {
        private readonly IPublisher publisher;
        private Queue<SystemEvent> eventsToProcess = new Queue<SystemEvent>();
        private List<EndedBreak> endedBreaks = new List<EndedBreak>();
        private StartedBreak? currentBreak;

        public BreakTracker(IPublisher publisher)
        {
            this.publisher = publisher;
        }

        public void AddEventToProcess(SystemEvent systemEvent)
        {
            if (systemEvent is null)
            {
                throw new ArgumentNullException(nameof(systemEvent));
            }

            eventsToProcess.Enqueue(systemEvent);
        }

        public void ProcessEvents()
        {
            while (eventsToProcess.Any())
            {
                var systemEvent = eventsToProcess.Dequeue();
                if (systemEvent is SystemLockedEvent)
                {
                    if (currentBreak is not null)
                    {
                        var endedBreak = currentBreak.EndBreak(systemEvent.OccuredOn);
                        endedBreaks.Add(endedBreak);
                        currentBreak = null;
                    }
                }
                else if (systemEvent is SystemUnlockedEvent)
                {
                    currentBreak = new StartedBreak(systemEvent.OccuredOn);
                }
            }
        }
    }
}
