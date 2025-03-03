using MediatR;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using TrackYourDay.Core.ApplicationTrackers.Breaks.Events;
using TrackYourDay.Core.SystemTrackers;
using TrackYourDay.Core.SystemTrackers.SystemStates;

namespace TrackYourDay.Core.ApplicationTrackers.Breaks
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
        private ConcurrentDictionary<Guid, EndedBreak> endedBreaks = new ConcurrentDictionary<Guid, EndedBreak>();
        private ConcurrentBag<RevokedBreak> revokedBreaks = new ConcurrentBag<RevokedBreak>();
        private StartedBreak? currentStartedBreak;
        private DateTime lastTimeOfActivity;

        public BreakTracker(IPublisher publisher, IClock clock, TimeSpan timeOfNoActivityToStartBreak, ILogger<BreakTracker> logger)
        {
            this.publisher = publisher;
            this.clock = clock;
            this.timeOfNoActivityToStartBreak = timeOfNoActivityToStartBreak;

            lastTimeOfActivity = this.clock.Now;
            this.logger = logger;
        }

        // <summary> This constructor is used only for testing purposes. It should be marked as internal/private in future.<summary>
        public BreakTracker(StartedBreak startedBreak, IPublisher publisher, IClock clock, TimeSpan timeOfNoActivityToStartBreak, ILogger<BreakTracker> logger) : this(publisher, clock, timeOfNoActivityToStartBreak, logger)
        {
            currentStartedBreak = startedBreak;
            this.logger = logger;
        }

        public void AddActivityToProcess(DateTime activityDate, SystemState activityType, Guid activityGuid)
        {
            if (activityType is null)
            {
                throw new ArgumentNullException(nameof(activityType));
            }

            var activityToProcess = new ActivityToProcess(activityDate, activityType, activityGuid);
            activitiesToProcess.Enqueue(activityToProcess);
            logger.LogInformation("Add: {ActivityToProcess}", activityToProcess);
            ProcessActivities();
        }

        private void AddActivityToProcess(StartedActivity startedActivity)
        {
            if (startedActivity is null)
            {
                throw new ArgumentNullException(nameof(startedActivity));
            }

            activitiesToProcessOld.Enqueue(startedActivity);
        }

        public void ProcessActivities()
        {
            while (activitiesToProcess.Any())
            {
                var activityToProcess = activitiesToProcess.Dequeue();

                logger.LogInformation("Process: {ActivityToProcess}", activityToProcess);
                // Starting break;
                if (currentStartedBreak is null)
                {
                    // Start Break If System Is Locked
                    if (activityToProcess.ActivityType is SystemLockedState)
                    {
                        currentStartedBreak = new StartedBreak(activityToProcess.ActivityDate, "System Locked");
                        logger.LogInformation("Start: {StartedBreak}", currentStartedBreak);
                        //this.lastTimeOfActivity = activityToProcess.ActivityDate;
                        publisher.Publish(new BreakStartedEvent(currentStartedBreak));
                        processedActivities.Add(activityToProcess);
                        continue;
                    }

                    // Start Break if there was no Activity for specified amount of time between events
                    var timeOfLackOfActivity = activityToProcess.ActivityDate - lastTimeOfActivity;
                    logger.LogInformation("Activity date: {ActivityDate}", activityToProcess.ActivityDate);
                    logger.LogInformation("Last time of activity: {LastTimeOfActivity}", lastTimeOfActivity);
                    logger.LogInformation("Time of lack of activity: {TimeOfLackOfActivity}", timeOfLackOfActivity);
                    if (timeOfLackOfActivity > timeOfNoActivityToStartBreak)
                    {
                        currentStartedBreak = new StartedBreak(lastTimeOfActivity, $"Lack of activity for {timeOfNoActivityToStartBreak.TotalMinutes} minutes");
                        logger.LogInformation("Start: {StartedBreak}", currentStartedBreak);
                        //this.lastTimeOfActivity = activityToProcess.ActivityDate;
                        publisher.Publish(new BreakStartedEvent(currentStartedBreak));
                        processedActivities.Add(activityToProcess);
                        continue;
                    }
                }

                // Ending break;
                if (currentStartedBreak is not null)
                {
                    if (activityToProcess.ActivityType is not SystemLockedState)
                    {
                        var endedBreak = currentStartedBreak.EndBreak(activityToProcess.ActivityDate);
                        logger.LogInformation("End: {EndedBreak}", endedBreak);
                        endedBreaks.TryAdd(endedBreak.Guid, endedBreak);
                        currentStartedBreak = null;
                        lastTimeOfActivity = activityToProcess.ActivityDate;
                        processedActivities.Add(activityToProcess);
                        publisher.Publish(new BreakEndedEvent(endedBreak));
                        continue;
                    }
                }

                lastTimeOfActivity = activityToProcess.ActivityDate;
            }

            // Start Break if there was no Activity for specified amount of time between last event and now
            if (currentStartedBreak is null &&
                clock.Now - lastTimeOfActivity > timeOfNoActivityToStartBreak)
            {
                currentStartedBreak = new StartedBreak(clock.Now, $"Lack of activity for {timeOfNoActivityToStartBreak.TotalMinutes} minutes");
                logger.LogInformation("Start: {StartedBreak}", currentStartedBreak);
                //this.lastTimeOfActivity = currentStartedBreak.BreakStartedAt;
                publisher.Publish(new BreakStartedEvent(currentStartedBreak));
            }            
        }

        public void RevokeBreak(Guid breakGuid, DateTime revokeDate)
        {
            // TODO: Change this approach as mentioned in tests for BreakTracker as it will cause issues in future
            endedBreaks.TryRemove(breakGuid, out var endedBreak);
            if (endedBreak is null)
            {
                throw new ArgumentException($"Break with guid {breakGuid} does not exist");
            }
            var revokedBreak = endedBreak.Revoke(revokeDate);
            revokedBreaks.Add(revokedBreak);

            publisher.Publish(new BreakRevokedEvent(revokedBreak));
        }

        public ReadOnlyCollection<EndedBreak> GetEndedBreaks()
        {
            return endedBreaks.ToList().Select(i => i.Value).ToList().AsReadOnly();
        }
    }
}
