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
        /// Amount of Time which Employee already worked
        /// </summary>
        // TODO: Add missing unit tests
        public TimeSpan TimeAlreadyActivelyWorkded { get; }

        /// <summary>
        /// Amount of Time which Employee should work to fullfill regulation requirements.
        /// This time does not include Breaks
        /// </summary>
        public TimeSpan TimeLeftToWorkActively { get; }

        /// <summary>
        /// Amount of Time which Employee worked more than regulation requirements
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

        public static Workday CreateBasedOn(IReadOnlyCollection<EndedActivity> endedActivities, IReadOnlyCollection<EndedBreak> endedBreaks)
        {
            //TODO: Verify because it is substituing Breaks
            var timeOfAllActivities = endedActivities.Aggregate(TimeSpan.Zero, (durationSum, activity) => durationSum + activity.GetDuration());
            
            var timeOfAllBreaks = endedBreaks.Aggregate(TimeSpan.Zero, (durationSum, @break) => durationSum + @break.BreakDuration);

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

            TimeSpan validBreaksUsed;
            if (timeOfAllBreaks.TotalSeconds > Config.AllowedBreakDuration.TotalSeconds)
            {
                validBreaksUsed = Config.AllowedBreakDuration;
            }
            else
            {
                validBreaksUsed = timeOfAllBreaks;
            }

            var timeLeftToWork = Config.WorkdayDuration - Config.AllowedBreakDuration - (timeOfAllActivities - timeOfAllBreaks);

            var overhours = timeLeftToWork < TimeSpan.Zero ? timeLeftToWork * -1 : TimeSpan.Zero;

            return new Workday(
                timeLeftToWork >= TimeSpan.Zero ? timeLeftToWork : TimeSpan.Zero,
                breaksLeft,
                overhours, 
                timeOfAllActivities, 
                timeOfAllBreaks,
                validBreaksUsed);
        }

        private Workday(TimeSpan timeLeftToWork, TimeSpan breaksLeft, TimeSpan overhours, TimeSpan timeOfAllActivities, TimeSpan timeOfAllBreaks, TimeSpan validBreaksUsed)
        {
            TimeLeftToWorkActively = timeLeftToWork;
            BreakTimeLeft = breaksLeft;
            OverhoursTime = overhours;
            TimeOfAllActivities = timeOfAllActivities;
            TimeOfAllBreaks = timeOfAllBreaks;
            ValidBreakTimeUsed = validBreaksUsed;
        }
    }
}
