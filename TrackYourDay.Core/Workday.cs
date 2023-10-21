using TrackYourDay.Core.Activities;
using TrackYourDay.Core.Breaks;

namespace TrackYourDay.Core
{
    public class Workday
    {
        public TimeSpan WorktimeLeft { get; }
        public TimeSpan BreaksLeft { get; }
        public TimeSpan Overhours { get; }

        public static Workday CreateBasedOn(List<EndedActivity> endedActivities, List<EndedBreak> endedBreaks)
        {

            return new Workday(TimeSpan.FromHours(8), TimeSpan.FromHours(8), TimeSpan.FromHours(8));
        }

        private Workday(TimeSpan worktimeLeft, TimeSpan breaksLeft, TimeSpan overhours)
        {
            WorktimeLeft = worktimeLeft;
            BreaksLeft = breaksLeft;
            Overhours = overhours;
        }
    }
}
