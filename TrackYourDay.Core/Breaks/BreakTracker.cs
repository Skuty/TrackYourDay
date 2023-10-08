using MediatR;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using TrackYourDay.Core.Activities;
using TrackYourDay.Core.Breaks.Notifications;

namespace TrackYourDay.Core.Breaks
{
    public class BreakTracker
    {
        private readonly ILogger<BreakTracker> logger;
        private readonly TimeSpan timeOfNoActivityToStartBreak;
        private readonly IPublisher publisher;
        private readonly IClock clock;
        private Queue<StartedActivity> activitiesToProcessOld = new Queue<StartedActivity>();
        //TODO: Imitate break on debugging and check processedActivities, there are activities in wrong order
        private Queue<ActivityToProcess> activitiesToProcess = new Queue<ActivityToProcess>();
        private List<ActivityToProcess> processedActivities = new List<ActivityToProcess>();
        private List<EndedBreak> endedBreaks = new List<EndedBreak>();
        private StartedBreak? currentStartedBreak;
        private DateTime lastTimeOfActivity;

        public BreakTracker(IPublisher publisher, IClock clock, TimeSpan timeOfNoActivityToStartBreak, ILogger<BreakTracker> logger)
        {
            this.publisher = publisher;
            this.clock = clock;
            this.timeOfNoActivityToStartBreak = timeOfNoActivityToStartBreak;

            this.lastTimeOfActivity = this.clock.Now;
            this.logger = logger;
        }

        // <summary> This constructor is used only for testing purposes. It should be marked as internal/private in future.<summary>
        public BreakTracker(StartedBreak startedBreak, IPublisher publisher, IClock clock, TimeSpan timeOfNoActivityToStartBreak, ILogger<BreakTracker> logger) : this(publisher, clock, timeOfNoActivityToStartBreak, logger)
        {
            this.currentStartedBreak = startedBreak;
            this.logger = logger;
        }

        public void AddActivityToProcess(DateTime activityDate, ActivityType activityType, Guid activityGuid)
        {
            if (activityType is null)
            {
                throw new ArgumentNullException(nameof(activityType));
            }

            var activityToProcess = new ActivityToProcess(activityDate, activityType, activityGuid);
            this.activitiesToProcess.Enqueue(activityToProcess);
            this.logger.LogInformation("Add: {ActivityToProcess}", activityToProcess);
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
                this.logger.LogInformation("Process: {ActivityToProcess}", activityToProcess);
                // Starting break;
                if (this.currentStartedBreak is null)
                {
                    // Start Break If System Is Locked
                    if (activityToProcess.ActivityType is SystemLockedActivityType)
                    {
                        this.currentStartedBreak = new StartedBreak(activityToProcess.ActivityDate, "System Locked");
                        this.logger.LogInformation("Start: {StartedBreak}", this.currentStartedBreak);
                        //this.lastTimeOfActivity = activityToProcess.ActivityDate;
                        this.publisher.Publish(new BreakStartedNotifcation(this.currentStartedBreak));
                        this.processedActivities.Add(activityToProcess);
                        continue;
                    }

                    // Start Break if there was no Activity for specified amount of time between events
                    var timeOfLackOfActivity = activityToProcess.ActivityDate - this.lastTimeOfActivity;
                    this.logger.LogInformation("Activity date: {ActivityDate}", activityToProcess.ActivityDate);
                    this.logger.LogInformation("Last time of activity: {LastTimeOfActivity}", this.lastTimeOfActivity);
                    this.logger.LogInformation("Time of lack of activity: {TimeOfLackOfActivity}", timeOfLackOfActivity);
                    if (timeOfLackOfActivity > timeOfNoActivityToStartBreak)
                    {
                        this.currentStartedBreak = new StartedBreak(this.lastTimeOfActivity, $"Lack of activity for {this.timeOfNoActivityToStartBreak.TotalMinutes} minutes");
                        this.logger.LogInformation("Start: {StartedBreak}", this.currentStartedBreak);
                        //this.lastTimeOfActivity = activityToProcess.ActivityDate;
                        this.publisher.Publish(new BreakStartedNotifcation(this.currentStartedBreak));
                        this.processedActivities.Add(activityToProcess);
                        continue;
                    }
                }

                // Ending break;
                if (this.currentStartedBreak is not null)
                {
                    if (activityToProcess.ActivityType is not SystemLockedActivityType)
                    {
                        var endedBreak = this.currentStartedBreak.EndBreak(activityToProcess.ActivityDate);
                        this.logger.LogInformation("End: {EndedBreak}", endedBreak);
                        this.endedBreaks.Add(endedBreak);
                        this.currentStartedBreak = null;
                        this.lastTimeOfActivity = activityToProcess.ActivityDate;
                        this.publisher.Publish(new BreakEndedNotifcation(endedBreak));
                        this.processedActivities.Add(activityToProcess);
                        continue;
                    }
                }

                this.lastTimeOfActivity = activityToProcess.ActivityDate;
            }

            // Start Break if there was no Activity for specified amount of time between last event and now
            if (this.currentStartedBreak is null && 
                (this.clock.Now - this.lastTimeOfActivity > this.timeOfNoActivityToStartBreak))
            {
                this.currentStartedBreak = new StartedBreak(clock.Now, $"Lack of activity for {this.timeOfNoActivityToStartBreak.TotalMinutes} minutes");
                this.logger.LogInformation("Start: {StartedBreak}", this.currentStartedBreak);
                //this.lastTimeOfActivity = currentStartedBreak.BreakStartedAt;
                this.publisher.Publish(new BreakStartedNotifcation(this.currentStartedBreak));
            }            
        }

        public ReadOnlyCollection<EndedBreak> GetEndedBreaks()
        {
            return this.endedBreaks.AsReadOnly();
        }
    }
}
