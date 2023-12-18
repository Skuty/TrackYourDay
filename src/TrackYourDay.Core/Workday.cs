﻿using TrackYourDay.Core.Activities;
using TrackYourDay.Core.Breaks;

namespace TrackYourDay.Core
{
    /// <summary>
    /// Represents Workday of hired Employee
    /// His details about worktime, breaks, etc.
    /// </summary>
    public record class Workday
    {
        /// <summary>
        /// Represents all Activities even that longterm which could be Breaks
        /// </summary>
        [Obsolete("Will be removed as it is not part of Workday")]
        public TimeSpan TimeOfAllActivities { get; }

        /// <summary>
        /// Represents all Breaks even that which are over the limit
        /// </summary>
        [Obsolete("Will be removed as it is not part of Workday")]
        public TimeSpan TimeOfAllBreaks { get; }

        /// <summary>
        /// Amount of Time which Employee should work to fullfill regulation requirements
        /// This time includes Breaks
        /// </summary>
        public TimeSpan OverallTimeLeftToWork { get; }

        /// <summary>
        /// Amount of Time which Employee should work to fullfill regulation requirements.
        /// This time does not include Breaks
        /// </summary>
        public TimeSpan TimeLeftToWorkActively { get; }

        /// <summary>
        /// Amount of Time which Employee already worked
        /// This time does not include Breaks
        /// </summary>
        public TimeSpan TimeAlreadyActivelyWorkded { get; }

        /// <summary>
        /// Amount of Time which Employee Actively worked more than regulation requirements
        /// </summary>
        public TimeSpan OverhoursTime { get; }

        /// <summary>
        /// Amount of Time left for Employee to use for Breaks
        /// </summary>
        public TimeSpan BreakTimeLeft { get; }

        /// <summary>
        /// Amount of Time defined by regulations used by Employee for Breaks
        /// </summary>
        public TimeSpan ValidBreakTimeUsed { get; }

        public Workday(
            TimeSpan timeOfAllActivities, 
            TimeSpan timeOfAllBreaks, 
            TimeSpan overallTimeLeftToWork, 
            TimeSpan timeLeftToWorkActively, 
            TimeSpan timeAlreadyActivelyWorkded, 
            TimeSpan overhoursTime, 
            TimeSpan breakTimeLeft, 
            TimeSpan validBreakTimeUsed)
        {
            this.TimeOfAllActivities = timeOfAllActivities;
            this.TimeOfAllBreaks = timeOfAllBreaks;
            this.OverallTimeLeftToWork = overallTimeLeftToWork;
            this.TimeLeftToWorkActively = timeLeftToWorkActively;
            this.TimeAlreadyActivelyWorkded = timeAlreadyActivelyWorkded;
            this.OverhoursTime = overhoursTime;
            this.BreakTimeLeft = breakTimeLeft;
            this.ValidBreakTimeUsed = validBreakTimeUsed;
        }

        public static Workday CreateBasedOn(IReadOnlyCollection<EndedActivity> endedActivities, IReadOnlyCollection<EndedBreak> endedBreaks)
        {
            var timeOfAllActivities = GetTimeOfAllActivities(endedActivities);
            var timeOfAllBreaks = GetTimeOfAllBreaks(endedBreaks);

            var validBreakTimeUsed = GetValidBreakTimeUsed(timeOfAllBreaks);
            var timeAlreadyActivelyWorkded = GetTimeAlreadyActivelyWorkded(timeOfAllActivities, timeOfAllBreaks, validBreakTimeUsed);

            var overallTimeLeftToWork = GetOverallTimeLeftToWork(timeAlreadyActivelyWorkded, validBreakTimeUsed);
            var timeLeftToWorkActively = GetTimeLeftToWorkActively(timeAlreadyActivelyWorkded);
            var overhoursTime = GetOverhours(timeAlreadyActivelyWorkded);
            var breakTimeLeft = GetTimeOfBreaksLeft(timeOfAllBreaks);


            return new Workday(
                timeOfAllActivities,
                timeOfAllBreaks,
                overallTimeLeftToWork,
                timeLeftToWorkActively,
                timeAlreadyActivelyWorkded,
                overhoursTime,
                breakTimeLeft,
                validBreakTimeUsed);
        }

        private static TimeSpan GetValidBreakTimeUsed(TimeSpan timeOfAllBreaks)
        {
            if (timeOfAllBreaks.TotalSeconds >= Config.AllowedBreakDuration.TotalSeconds)
            {
                return Config.AllowedBreakDuration;
            }
            else
            {
                return timeOfAllBreaks;
            }
        }

        private static TimeSpan GetTimeAlreadyActivelyWorkded(TimeSpan timeOfAllActivities, TimeSpan timeOfAllBreaks, TimeSpan validBreakTimeUsed)
        {
            var timeAlreadyActivelyWorkded = timeOfAllActivities - timeOfAllBreaks;
            return timeAlreadyActivelyWorkded >= TimeSpan.Zero ? timeAlreadyActivelyWorkded : TimeSpan.Zero;
        }

        private static TimeSpan GetTimeLeftToWorkActively(TimeSpan timeAlreadyActivelyWorkded)
        {
            //timealreadyActivelyWorked is negative
            var timeLeftToWorkActively = Config.WorkdayDuration - Config.AllowedBreakDuration - timeAlreadyActivelyWorkded;
            return timeLeftToWorkActively >= TimeSpan.Zero ? timeLeftToWorkActively : TimeSpan.Zero;
        }

        private static TimeSpan GetOverallTimeLeftToWork(TimeSpan timeAlreadyActivelyWorkded, TimeSpan validBreakTimeUsed)
        {
            var overallTinmeLeftToWork = Config.WorkdayDuration - timeAlreadyActivelyWorkded - validBreakTimeUsed;
            return overallTinmeLeftToWork >= TimeSpan.Zero ? overallTinmeLeftToWork : TimeSpan.Zero;
        }

        private static TimeSpan GetOverhours(TimeSpan timeAlreadyActivelyWorkded)
        {
            var overhours = Config.WorkdayDuration - Config.AllowedBreakDuration - timeAlreadyActivelyWorkded;

            return overhours < TimeSpan.Zero ? overhours * -1 : TimeSpan.Zero;
        }

        private static TimeSpan GetTimeLeftToWork(TimeSpan timeOfAllActivities, TimeSpan timeOfAllBreaks)
        {
            var timeLeftToWork = Config.WorkdayDuration - Config.AllowedBreakDuration - (timeOfAllActivities - timeOfAllBreaks);
            return timeLeftToWork >= TimeSpan.Zero ? timeLeftToWork : TimeSpan.Zero;
        }

        private static TimeSpan GetTimeOfValidBreaksUsed(TimeSpan timeOfAllBreaks)
        {
            TimeSpan validBreaksUsed;
            if (timeOfAllBreaks.TotalSeconds >= Config.AllowedBreakDuration.TotalSeconds)
            {
                return Config.AllowedBreakDuration;
            }
            else
            {
                return timeOfAllBreaks;
            }
        }

        private static TimeSpan GetTimeOfBreaksLeft(TimeSpan timeOfAllBreaks)
        {
            //TODO: Extract static config from here 
            if (timeOfAllBreaks.TotalSeconds > Config.AllowedBreakDuration.TotalSeconds)
            {
                return TimeSpan.Zero;
            }
            else
            {
                return Config.AllowedBreakDuration - timeOfAllBreaks;
            }
        }

        private static TimeSpan GetTimeOfAllBreaks(IReadOnlyCollection<EndedBreak> endedBreaks)
        {
            return endedBreaks.Aggregate(TimeSpan.Zero, (durationSum, @break) => durationSum + @break.BreakDuration);
        }

        private static TimeSpan GetTimeOfAllActivities(IReadOnlyCollection<EndedActivity> endedActivities)
        {
            return endedActivities.Aggregate(TimeSpan.Zero, (durationSum, activity) => durationSum + activity.GetDuration());
        }

    }
}