using TrackYourDay.Core.Activities;
using TrackYourDay.Core.Breaks;

namespace TrackYourDay.Core
{
    // Existing assumptions are conflicting with each other

    //TODO: Add tests for not tested parts or extract them

    /// <summary>
    /// Represents Workday of hired Employee
    /// His details about worktime, breaks, etc.
    /// </summary>
    public class Workday
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

        private Workday(TimeSpan timeLeftToWork, TimeSpan breaksLeft, TimeSpan overhours, TimeSpan timeOfAllActivities, TimeSpan timeOfAllBreaks, TimeSpan validBreaksUsed)
        {
            TimeLeftToWorkActively = timeLeftToWork;
            BreakTimeLeft = breaksLeft;
            OverhoursTime = overhours;
            TimeOfAllActivities = timeOfAllActivities;
            TimeOfAllBreaks = timeOfAllBreaks;
            ValidBreakTimeUsed = validBreaksUsed;
        }

        public static Workday CreateBasedOn(IReadOnlyCollection<EndedActivity> endedActivities, IReadOnlyCollection<EndedBreak> endedBreaks)
        {
            var timeOfAllActivities = GetTimeOfAllActivities(endedActivities);
            var timeOfAllBreaks = GetTimeOfAllBreaks(endedBreaks);
            var breakTimeLeft = GetTimeOfBreaksLeft(timeOfAllBreaks);
            var validBreaksUsed = GetTimeOfValidBreaksUsed(timeOfAllBreaks);
            var timeLeftToWork = GetTimeLeftToWork(timeOfAllActivities, timeOfAllBreaks);
            var overhours = GetOverhours(timeLeftToWork);

            return new Workday(
                timeLeftToWork,
                breakTimeLeft,
                overhours,
                timeOfAllActivities,
                timeOfAllBreaks,
                validBreaksUsed);
        }

        private static TimeSpan GetOverhours(TimeSpan timeLeftToWork)
        {
            return timeLeftToWork < TimeSpan.Zero ? timeLeftToWork * -1 : TimeSpan.Zero;
        }

        private static TimeSpan GetTimeLeftToWork(TimeSpan timeOfAllActivities, TimeSpan timeOfAllBreaks)
        {
            var timeLeftToWork = Config.WorkdayDuration - Config.AllowedBreakDuration - (timeOfAllActivities - timeOfAllBreaks);
            return timeLeftToWork >= TimeSpan.Zero ? timeLeftToWork : TimeSpan.Zero;
        }

        private static TimeSpan GetTimeOfValidBreaksUsed(TimeSpan timeOfAllBreaks)
        {
            TimeSpan validBreaksUsed;
            if (timeOfAllBreaks.TotalSeconds > Config.AllowedBreakDuration.TotalSeconds)
            {
                validBreaksUsed = Config.AllowedBreakDuration;
            }
            else
            {
                validBreaksUsed = timeOfAllBreaks;
            }

            return validBreaksUsed;
        }

        private static TimeSpan GetTimeOfBreaksLeft(TimeSpan timeOfAllBreaks)
        {
            TimeSpan breaksLeft;
            //TODO: Extract static config from here 
            if (timeOfAllBreaks.TotalSeconds > Config.AllowedBreakDuration.TotalSeconds)
            {
                breaksLeft = TimeSpan.Zero;
            }
            else
            {
                breaksLeft = Config.AllowedBreakDuration - timeOfAllBreaks;
            }

            return breaksLeft;
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
