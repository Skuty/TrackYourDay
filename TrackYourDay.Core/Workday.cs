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
        /// Amount of Time which Employee already worked
        /// </summary>
        /// 
        public TimeSpan TimeAlreadyWorkded { get; }

        /// <summary>
        /// Amount of Time which Employee should work to fullfill regulation requirements.
        /// This time does not include Breaks
        /// </summary>
        public TimeSpan TimeLeftToWork { get; }

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


            //TODO: Correct this to ignore activities that were causing breaks etc.
            var worktimeLeft = Config.WorkdayDuration - validBreaksUsed - timeOfAllActivities;


            var overhours = worktimeLeft < TimeSpan.Zero ? worktimeLeft * -1 : TimeSpan.Zero;

            return new Workday(
                worktimeLeft >= TimeSpan.Zero ? worktimeLeft : TimeSpan.Zero,
                breaksLeft,
                overhours, 
                timeOfAllActivities, 
                timeOfAllBreaks,
                validBreaksUsed);
        }

        private Workday(TimeSpan worktimeLeft, TimeSpan breaksLeft, TimeSpan overhours, TimeSpan timeOfAllActivities, TimeSpan timeOfAllBreaks, TimeSpan validBreaksUsed)
        {
            TimeLeftToWork = worktimeLeft;
            BreakTimeLeft = breaksLeft;
            OverhoursTime = overhours;
            TimeOfAllActivities = timeOfAllActivities;
            TimeOfAllBreaks = timeOfAllBreaks;
            ValidBreakTimeUsed = validBreaksUsed;
        }
    }
}
