using System.Collections.ObjectModel;
using TrackYourDay.Core.Activities;
using TrackYourDay.Core.Breaks;

namespace TrackYourDay.Core
{
    //TODO: Reconisder and write down what is the purpose of this class
    // What is WorkDay, worktimeleft, allWorkTime, etc
    // Existing assumptions are conflicting with each other

    //TODO: Add tests for not tested parts or extract them
    public class Workday
    {
        public TimeSpan TimeOfAllActivities { get; }

        public TimeSpan TimeOfAllBreaks { get; }

        public TimeSpan BreaksLeft { get; }

        public TimeSpan ValidBreaksUsed { get; }

        public TimeSpan WorktimeLeft { get; }
        
        public TimeSpan Overhours { get; }

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
            WorktimeLeft = worktimeLeft;
            BreaksLeft = breaksLeft;
            Overhours = overhours;
            TimeOfAllActivities = timeOfAllActivities;
            TimeOfAllBreaks = timeOfAllBreaks;
            ValidBreaksUsed = validBreaksUsed;
        }
    }
}
