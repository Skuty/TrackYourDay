using MediatR;
using System.Collections.ObjectModel;
using TrackYourDay.Core.Activities;
using TrackYourDay.Core.Breaks.Notifications;

namespace TrackYourDay.Core.Breaks
{
    public class BreakTracker
    {
        private readonly TimeSpan timeOfNoActivityToStartBreak;
        private readonly IPublisher publisher;
        private readonly IClock clock;
        private Queue<StartedActivity> activitiesToProcessOld = new Queue<StartedActivity>();
        private Queue<ActivityToProcess> activitiesToProcess = new Queue<ActivityToProcess>();
        private List<EndedBreak> endedBreaks = new List<EndedBreak>();
        private StartedBreak? currentStartedBreak;
        private DateTime lastTimeOfActivity;

        public BreakTracker(IPublisher publisher, IClock clock, TimeSpan timeOfNoActivityToStartBreak)
        {
            this.publisher = publisher;
            this.clock = clock;
            this.timeOfNoActivityToStartBreak = timeOfNoActivityToStartBreak;

            this.lastTimeOfActivity = this.clock.Now;
        }

        // <summary> This constructor is used only for testing purposes. It should be marked as internal/private in future.<summary>
        public BreakTracker(StartedBreak startedBreak, IPublisher publisher, IClock clock, TimeSpan timeOfNoActivityToStartBreak) : this(publisher, clock, timeOfNoActivityToStartBreak)
        {
            this.currentStartedBreak = startedBreak;
        }

        public void AddActivityToProcess(DateTime activityDate, ActivityType activityType)
        {
            if (activityType is null)
            {
                throw new ArgumentNullException(nameof(activityType));
            }

            var activityToProcess = new ActivityToProcess(activityDate, activityType);

            this.activitiesToProcess.Enqueue(activityToProcess);
            this.ProcessActivities();
        }

        private void AddActivityToProcess(StartedActivity startedActivity)
        {
            if (startedActivity is null)
            {
                throw new ArgumentNullException(nameof(startedActivity));
            }

            this.activitiesToProcessOld.Enqueue(startedActivity);
        }

        public void ProcessActivities()
        {
            while (this.activitiesToProcess.Any())
            {
                var activityToProcess = this.activitiesToProcess.Dequeue();
                // Starting break;
                if (this.currentStartedBreak is null)
                {
                    // Start Break If System Is Locked
                    if (activityToProcess.ActivityType is SystemLockedActivityType)
                    {
                        this.currentStartedBreak = new StartedBreak(activityToProcess.ActivityDate, "System Locked");
                        this.lastTimeOfActivity = activityToProcess.ActivityDate;
                        this.publisher.Publish(new BreakStartedNotifcation(this.currentStartedBreak));
                        continue;
                    }

                    // Start Break if there was no Activity for specified amount of time between events
                    if (activityToProcess.ActivityDate - this.lastTimeOfActivity > timeOfNoActivityToStartBreak)
                    {
                        this.currentStartedBreak = new StartedBreak(activityToProcess.ActivityDate, $"Lack of activity for {this.timeOfNoActivityToStartBreak.TotalMinutes} minutes");
                        this.lastTimeOfActivity = activityToProcess.ActivityDate;
                        this.publisher.Publish(new BreakStartedNotifcation(this.currentStartedBreak));
                        continue;
                    }
                }

                // Ending break;
                if (this.currentStartedBreak is not null)
                {
                    if (activityToProcess.ActivityType is not SystemLockedActivityType)
                    {
                        var endedBreak = this.currentStartedBreak.EndBreak(activityToProcess.ActivityDate);
                        this.endedBreaks.Add(endedBreak);
                        this.currentStartedBreak = null;
                        this.lastTimeOfActivity = endedBreak.BreakEndedAt;
                        this.publisher.Publish(new BreakEndedNotifcation(endedBreak));
                        continue;
                    }
                }
            }

            // Start Break if there was no Activity for specified amount of time between last event and now
            if (this.currentStartedBreak is null && 
                (this.clock.Now - this.lastTimeOfActivity > this.timeOfNoActivityToStartBreak))
            {
                this.currentStartedBreak = new StartedBreak(clock.Now, $"Lack of activity for {this.timeOfNoActivityToStartBreak.TotalMinutes} minutes");
                this.lastTimeOfActivity = currentStartedBreak.BreakStartedAt;
                this.publisher.Publish(new BreakStartedNotifcation(this.currentStartedBreak));
            }
        }

        public ReadOnlyCollection<EndedBreak> GetEndedBreaks()
        {
            return this.endedBreaks.AsReadOnly();
        }
    }
}
