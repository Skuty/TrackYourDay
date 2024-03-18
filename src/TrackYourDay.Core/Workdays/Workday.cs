using TrackYourDay.Core.Activities;
using TrackYourDay.Core.Activities.Events;
using TrackYourDay.Core.Breaks;
using TrackYourDay.Core.Breaks.Events;
using TrackYourDay.Core.Settings;

namespace TrackYourDay.Core.Workdays
{
    /// <summary>
    /// Represents Workday of hired Employee
    /// His details about worktime, breaks, etc.
    /// </summary>
    public record class Workday
    {
        public DateOnly Date { get; init; }

        [Obsolete("Temporary. In future remove and initialize Workday withi inital values")]
        private WorkdayDefinition WorkdayDefinition { get; init; }

        /// <summary>
        /// Represents all Activities even that longterm which could be Breaks
        /// </summary>
        [Obsolete("Will be removed as it is not part of Workday")]
        public TimeSpan TimeOfAllActivities { get; init; }

        /// <summary>
        /// Represents all Breaks even that which are over the limit
        /// </summary>
        [Obsolete("Will be removed as it is not part of Workday")]
        public TimeSpan TimeOfAllBreaks { get; init; }

        /// <summary>
        /// Amount of Time which Employee should work to fullfill regulation requirements
        /// This time includes Breaks
        /// </summary>
        public TimeSpan OverallTimeLeftToWork { get; init; }

        /// <summary>
        /// Amount of Time which Employee should work to fullfill regulation requirements.
        /// This time does not include Breaks
        /// </summary>
        public TimeSpan TimeLeftToWorkActively { get; init; }

        /// <summary>
        /// Amount of Time which Employee already worked
        /// This time does not include Breaks
        /// </summary>
        public TimeSpan TimeAlreadyActivelyWorkded { get; init; }

        /// <summary>
        /// Amount of Time which Employee Actively worked more than regulation requirements
        /// </summary>
        public TimeSpan OverhoursTime { get; init; }

        /// <summary>
        /// Amount of Time left for Employee to use for Breaks
        /// </summary>
        public TimeSpan BreakTimeLeft { get; init; }

        /// <summary>
        /// Amount of Time defined by regulations used by Employee for Breaks
        /// </summary>
        public TimeSpan ValidBreakTimeUsed { get; init; }

        // TODO: Createowrkd ay without workday definition and get rid ofcoupling to workday
        private Workday() { }

        private Workday(
            WorkdayDefinition workdayDefinition,
            TimeSpan timeOfAllActivities,
            TimeSpan timeOfAllBreaks,
            TimeSpan overallTimeLeftToWork,
            TimeSpan timeLeftToWorkActively,
            TimeSpan timeAlreadyActivelyWorkded,
            TimeSpan overhoursTime,
            TimeSpan breakTimeLeft,
            TimeSpan validBreakTimeUsed)
        {
            WorkdayDefinition = workdayDefinition;
            TimeOfAllActivities = timeOfAllActivities;
            TimeOfAllBreaks = timeOfAllBreaks;
            OverallTimeLeftToWork = overallTimeLeftToWork;
            TimeLeftToWorkActively = timeLeftToWorkActively;
            TimeAlreadyActivelyWorkded = timeAlreadyActivelyWorkded;
            OverhoursTime = overhoursTime;
            BreakTimeLeft = breakTimeLeft;
            ValidBreakTimeUsed = validBreakTimeUsed;
        }

        public static Workday Create(WorkdayDefinition workdayDefinition)
        {
            return new Workday()
        }

        public static Workday CreateBasedOn(WorkdayDefinition workdayDefinition, IReadOnlyCollection<EndedActivity> endedActivities, IReadOnlyCollection<EndedBreak> endedBreaks)
        {
            var timeOfAllActivities = GetTimeOfAllActivities(endedActivities);
            var timeOfAllBreaks = GetTimeOfAllBreaks(endedBreaks);

            var validBreakTimeUsed = GetValidBreakTimeUsed(timeOfAllBreaks, workdayDefinition);
            var timeAlreadyActivelyWorkded = GetTimeAlreadyActivelyWorkded(timeOfAllActivities, timeOfAllBreaks, validBreakTimeUsed);

            var overallTimeLeftToWork = GetOverallTimeLeftToWork(timeAlreadyActivelyWorkded, validBreakTimeUsed, workdayDefinition);
            var timeLeftToWorkActively = GetTimeLeftToWorkActively(timeAlreadyActivelyWorkded, workdayDefinition);
            var overhoursTime = GetOverhours(timeAlreadyActivelyWorkded, workdayDefinition);
            var breakTimeLeft = GetTimeOfBreaksLeft(timeOfAllBreaks, workdayDefinition);


            return new Workday(
                workdayDefinition,
                timeOfAllActivities,
                timeOfAllBreaks,
                overallTimeLeftToWork,
                timeLeftToWorkActively,
                timeAlreadyActivelyWorkded,
                overhoursTime,
                breakTimeLeft,
                validBreakTimeUsed);
        }

        private static TimeSpan GetValidBreakTimeUsed(TimeSpan timeOfAllBreaks, WorkdayDefinition workdayDefinition)
        {
            if (timeOfAllBreaks.TotalSeconds >= workdayDefinition.AllowedBreakDuration.TotalSeconds)
            {
                return workdayDefinition.AllowedBreakDuration;
            }
            else
            {
                return timeOfAllBreaks;
            }
        }

        [Obsolete("Replaced by Include Event approach")]
        private static TimeSpan GetTimeAlreadyActivelyWorkded(TimeSpan timeOfAllActivities, TimeSpan timeOfAllBreaks, TimeSpan validBreakTimeUsed)
        {
            var timeAlreadyActivelyWorkded = timeOfAllActivities - timeOfAllBreaks;
            return timeAlreadyActivelyWorkded >= TimeSpan.Zero ? timeAlreadyActivelyWorkded : TimeSpan.Zero;
        }

        private TimeSpan GetTimeAlreadyActivelyWorkded_New(TimeSpan timeOfAllActivities, TimeSpan timeOfAllBreaks)
        {
            var timeAlreadyActivelyWorkded = timeOfAllActivities - timeOfAllBreaks;
            return timeAlreadyActivelyWorkded >= TimeSpan.Zero ? timeAlreadyActivelyWorkded : TimeSpan.Zero;
        }


        private static TimeSpan GetTimeLeftToWorkActively(TimeSpan timeAlreadyActivelyWorkded, WorkdayDefinition workdayDefinition)
        {
            //timealreadyActivelyWorked is negative
            var timeLeftToWorkActively = workdayDefinition.WorkdayDuration - workdayDefinition.AllowedBreakDuration - timeAlreadyActivelyWorkded;
            return timeLeftToWorkActively >= TimeSpan.Zero ? timeLeftToWorkActively : TimeSpan.Zero;
        }
        private TimeSpan GetTimeLeftToWorkActively()
        {
            throw new NotImplementedException();
        }

        private static TimeSpan GetOverallTimeLeftToWork(TimeSpan timeAlreadyActivelyWorkded, TimeSpan validBreakTimeUsed, WorkdayDefinition workdayDefinition)
        {
            var overallTinmeLeftToWork = workdayDefinition.WorkdayDuration - timeAlreadyActivelyWorkded - validBreakTimeUsed;
            return overallTinmeLeftToWork >= TimeSpan.Zero ? overallTinmeLeftToWork : TimeSpan.Zero;
        }

        private TimeSpan GetOverallTimeLeftToWork()
        {
            throw new NotImplementedException();
        }

        private static TimeSpan GetOverhours(TimeSpan timeAlreadyActivelyWorkded, WorkdayDefinition workdayDefinition)
        {
            var overhours = workdayDefinition.WorkdayDuration - workdayDefinition.AllowedBreakDuration - timeAlreadyActivelyWorkded;

            return overhours < TimeSpan.Zero ? overhours * -1 : TimeSpan.Zero;
        }

        private TimeSpan GetOverhoursTime()
        {
            throw new NotImplementedException();
        }

        private static TimeSpan GetTimeLeftToWork(TimeSpan timeOfAllActivities, TimeSpan timeOfAllBreaks, WorkdayDefinition workdayDefinition)
        {
            var timeLeftToWork = workdayDefinition.WorkdayDuration - workdayDefinition.AllowedBreakDuration - (timeOfAllActivities - timeOfAllBreaks);
            return timeLeftToWork >= TimeSpan.Zero ? timeLeftToWork : TimeSpan.Zero;
        }

        private static TimeSpan GetTimeOfValidBreaksUsed(TimeSpan timeOfAllBreaks, WorkdayDefinition workdayDefinition)
        {
            TimeSpan validBreaksUsed;
            if (timeOfAllBreaks.TotalSeconds >= workdayDefinition.AllowedBreakDuration.TotalSeconds)
            {
                return workdayDefinition.AllowedBreakDuration;
            }
            else
            {
                return timeOfAllBreaks;
            }
        }

        private static TimeSpan GetTimeOfBreaksLeft(TimeSpan timeOfAllBreaks, WorkdayDefinition workdayDefinition)
        {
            //TODO: Extract static config from here 
            if (timeOfAllBreaks.TotalSeconds > workdayDefinition.AllowedBreakDuration.TotalSeconds)
            {
                return TimeSpan.Zero;
            }
            else
            {
                return workdayDefinition.AllowedBreakDuration - timeOfAllBreaks;
            }
        }

        private static TimeSpan GetTimeOfAllBreaks(IReadOnlyCollection<EndedBreak> endedBreaks)
        {
            return endedBreaks.Aggregate(TimeSpan.Zero, (durationSum, @break) => durationSum + @break.BreakDuration);
        }

        [Obsolete("Replaced by Include Event approach")]
        private static TimeSpan GetTimeOfAllActivities(IReadOnlyCollection<EndedActivity> endedActivities)
        {
            return endedActivities.Aggregate(TimeSpan.Zero, (durationSum, activity) => durationSum + activity.GetDuration());
        }

        private TimeSpan GetTimeOfAllActivities(TimeSpan timeOfAllActivities, TimeSpan newActivity)
        {
            return timeOfAllActivities - newActivity;
        }

        //TODO: Zweryfikowaćdwa podejścia:
        /// Zaaplikować każdy event do dnia lub tylko składową eventu per metoda
        internal Workday Include(PeriodicActivityEndedEvent notification)
        {
            return new Workday(this)
            {
                TimeOfAllActivities = this.GetTimeOfAllActivities(
                    this.TimeOfAllActivities, notification.EndedActivity.GetDuration()),
                
                TimeAlreadyActivelyWorkded = this.GetTimeAlreadyActivelyWorkded(),
                OverallTimeLeftToWork = this.GetOverallTimeLeftToWork(),
                TimeLeftToWorkActively = this.GetTimeLeftToWorkActively(),
                OverhoursTime = this.GetOverhoursTime(),
            };
        }

        internal Workday Include(BreakEndedEvent notification)
        {
            throw new NotImplementedException();
        }

        internal Workday Include(BreakRevokedEvent notification)
        {
            throw new NotImplementedException();
        }
    }
}
