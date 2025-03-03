using System.Reflection.Metadata.Ecma335;
using TrackYourDay.Core.ApplicationTrackers.Breaks;
using TrackYourDay.Core.SystemTrackers;
using TrackYourDay.Core.SystemTrackers.SystemStates;

namespace TrackYourDay.Core.Insights.Workdays
{
    /// <summary>
    /// Represents Workday of hired Employee
    /// His details about worktime, breaks, etc.
    /// </summary>
    public record class Workday
    {
        // TODO: this probably should not exists but somehow readmodel at app startup have to be created correctly
        // Maybe delayed notificaion startup? or explicit order of registration/creation?
        public static Workday CreateEmpty(DateOnly date)
        {
            // This should be really empty, not just initialized
            var workdayDefinition = WorkdayDefinition.CreateSampleCompanyDefinition();
            var workday = new Workday()
            {
                Date = date,
                WorkdayDefinition = workdayDefinition,
                BreakTimeLeft = workdayDefinition.AllowedBreakDuration,
                TimeLeftToWorkActively = workdayDefinition.WorkdayDuration - workdayDefinition.AllowedBreakDuration,
                OverallTimeLeftToWork = workdayDefinition.WorkdayDuration,
            };

            return workday;
        }

        public DateOnly Date { get; init; }

        [Obsolete("Temporary. In future remove and initialize Workday withi inital values")]
        private WorkdayDefinition WorkdayDefinition { get; init; }

        /// <summary>
        /// Represents all Activities even that longterm which could be Breaks
        /// </summary>
        [Obsolete("Will be removed as it is not part of Workday")]
        public TimeSpan TimeOfAllActivities { get; init; }

        /// <summary>
        /// Represents all Activities even that longterm which could be Breaks
        /// </summary>
        /// <remarks>
        /// Its equivalent of the public property that does not have to be public.
        /// As private property it can be helpfull
        /// </remarks>
        private TimeSpan timeOfAllActivities { get; set; }


        /// <summary>
        /// Represents all Breaks even that which are over the limit
        /// </summary>
        [Obsolete("Will be removed as it is not part of Workday")]
        public TimeSpan TimeOfAllBreaks { get; init; }

        /// <summary>
        /// Represents all Activities even that longterm which could be Breaks
        /// </summary>
        /// <remarks>
        /// Its equivalent of the public property that does not have to be public.
        /// As private property it can be helpfull
        /// </remarks>
        public TimeSpan timeOfAllBreaks { get; set; }

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

        private TimeSpan timeLeftToWorkActively { get; set; }

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

        public static Workday CreateForDate(DateOnly date, WorkdayDefinition workdayDefinition)
        {
            return new Workday()
            {
                Date = date,
                WorkdayDefinition = workdayDefinition,
                TimeOfAllActivities = TimeSpan.Zero,
                TimeOfAllBreaks = TimeSpan.Zero,
                OverallTimeLeftToWork = workdayDefinition.WorkdayDuration,
                TimeLeftToWorkActively = workdayDefinition.WorkdayDuration - workdayDefinition.AllowedBreakDuration,
                TimeAlreadyActivelyWorkded = TimeSpan.Zero,
                OverhoursTime = TimeSpan.Zero,
                BreakTimeLeft = workdayDefinition.AllowedBreakDuration,
                ValidBreakTimeUsed = TimeSpan.Zero
            };
        }

        public static Workday CreateBasedOn(WorkdayDefinition workdayDefinition, IReadOnlyCollection<EndedActivity> endedActivities, IReadOnlyCollection<EndedBreak> endedBreaks)
        {
            var workday = CreateForDate(DateOnly.FromDateTime(DateTime.Today), workdayDefinition);

            foreach (var endedActivity in endedActivities)
            {
                workday = workday.Include(endedActivity);
            }

            foreach (var endedBreak in endedBreaks)
            {
                workday = workday.Include(endedBreak);
            }

            return workday;
        }

        public static Workday CreateBasedOnOld(WorkdayDefinition workdayDefinition, IReadOnlyCollection<EndedActivity> endedActivities, IReadOnlyCollection<EndedBreak> endedBreaks)
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
        internal Workday Include(EndedActivity endedActivity)
        {
            this.timeOfAllActivities += endedActivity.GetDuration();
            var timeOfAllActivities = TimeOfAllActivities + endedActivity.GetDuration();
            var timeOfAllBreaks = TimeOfAllBreaks;

            var timeLeftToWorkActively =
                WorkdayDefinition.WorkdayDuration 
                - WorkdayDefinition.AllowedBreakDuration 
                - TimeAlreadyActivelyWorkded
                - endedActivity.GetDuration();
            this.timeLeftToWorkActively = timeLeftToWorkActively;
            
            var timeAlreadyActivelyWorkded = TimeAlreadyActivelyWorkded + endedActivity.GetDuration();
            var overhoursTime = WorkdayDefinition.WorkdayDuration - WorkdayDefinition.AllowedBreakDuration - timeAlreadyActivelyWorkded;
            var breakTimeLeft = BreakTimeLeft;
            var validBreakTimeUsed = ValidBreakTimeUsed;
            var overallTimeLeftToWork = WorkdayDefinition.WorkdayDuration - timeAlreadyActivelyWorkded - validBreakTimeUsed;

            var result = new Workday(this)
            {
                TimeOfAllActivities = timeOfAllActivities,
                TimeOfAllBreaks = timeOfAllBreaks,
                OverallTimeLeftToWork = overallTimeLeftToWork >= TimeSpan.Zero ? overallTimeLeftToWork : TimeSpan.Zero,
                TimeLeftToWorkActively = timeLeftToWorkActively >= TimeSpan.Zero ? timeLeftToWorkActively : TimeSpan.Zero,
                TimeAlreadyActivelyWorkded = timeAlreadyActivelyWorkded,
                OverhoursTime = overhoursTime < TimeSpan.Zero ? overhoursTime * -1 : TimeSpan.Zero,
                BreakTimeLeft = breakTimeLeft,
                ValidBreakTimeUsed = validBreakTimeUsed
            };

            return result;
        }

        internal Workday Include(EndedBreak endedBreak)
        {
            var timeOfAllActivities = TimeOfAllActivities;
            var timeOfAllBreaks = TimeOfAllBreaks + endedBreak.BreakDuration;
            this.timeOfAllBreaks = timeOfAllBreaks;
            var timeLeftToWorkActively = this.timeLeftToWorkActively + endedBreak.BreakDuration; 
            var timeAlreadyActivelyWorkded = TimeAlreadyActivelyWorkded - endedBreak.BreakDuration;
            var overhoursTime = WorkdayDefinition.WorkdayDuration - WorkdayDefinition.AllowedBreakDuration - timeAlreadyActivelyWorkded;
            var breakTimeLeft = BreakTimeLeft - endedBreak.BreakDuration;
            var validBreakTimeUsed = ValidBreakTimeUsed + endedBreak.BreakDuration;
            if (timeOfAllBreaks.TotalSeconds >= WorkdayDefinition.AllowedBreakDuration.TotalSeconds)
            {
                validBreakTimeUsed = WorkdayDefinition.AllowedBreakDuration;
            }
            else
            {
                validBreakTimeUsed = timeOfAllBreaks;
            }
            var overallTimeLeftToWork = WorkdayDefinition.WorkdayDuration - (timeAlreadyActivelyWorkded >= TimeSpan.Zero ? timeAlreadyActivelyWorkded : TimeSpan.Zero) - validBreakTimeUsed;

            return new Workday(this)
            {
                TimeOfAllActivities = timeOfAllActivities,
                TimeOfAllBreaks = timeOfAllBreaks,
                OverallTimeLeftToWork = overallTimeLeftToWork,
                TimeLeftToWorkActively = timeLeftToWorkActively >= TimeSpan.Zero ? timeLeftToWorkActively : TimeSpan.Zero,
                TimeAlreadyActivelyWorkded = timeAlreadyActivelyWorkded >= TimeSpan.Zero ? timeAlreadyActivelyWorkded : TimeSpan.Zero,
                OverhoursTime = overhoursTime < TimeSpan.Zero ? overhoursTime * -1 : TimeSpan.Zero,
                BreakTimeLeft = breakTimeLeft >= TimeSpan.Zero ? breakTimeLeft : TimeSpan.Zero,
                ValidBreakTimeUsed = validBreakTimeUsed
            };
        }

        internal Workday Include(RevokedBreak revokedBreak)
        {
            // TODO: Because calculating breaks that are lost if values go below zero
            // Workday should be extended to contain all data (not events) breakTimeUsed, BreakTimeRevoked, etc.
            var timeOfAllActivities = TimeOfAllActivities;
            var timeOfAllBreaks = TimeOfAllBreaks - revokedBreak.EndedBreak.BreakDuration;
            var overallTimeLeftToWork = OverallTimeLeftToWork; // ToFix as above
            var timeLeftToWorkActively = TimeLeftToWorkActively;
            var timeAlreadyActivelyWorkded = TimeAlreadyActivelyWorkded;
            var overhoursTime = OverhoursTime;
            var breakTimeLeft = WorkdayDefinition.AllowedBreakDuration - BreakTimeLeft + revokedBreak.EndedBreak.BreakDuration; // ToFix
            var validBreakTimeUsed = ValidBreakTimeUsed; // ToFix


            return new Workday(this)
            {
                TimeOfAllActivities = timeOfAllActivities,
                TimeOfAllBreaks = timeOfAllBreaks,
                OverallTimeLeftToWork = overallTimeLeftToWork,
                TimeLeftToWorkActively = timeLeftToWorkActively,
                TimeAlreadyActivelyWorkded = timeAlreadyActivelyWorkded,
                OverhoursTime = overhoursTime,
                BreakTimeLeft = breakTimeLeft > TimeSpan.Zero ? breakTimeLeft : TimeSpan.Zero,
                ValidBreakTimeUsed = validBreakTimeUsed
            };
        }

        [Obsolete("Used just for mocks")]
        public static Workday CreateSample()
        {
            var sampleDate = DateOnly.FromDateTime(DateTime.Today);
            var sampleWorkdayDefinition = WorkdayDefinition.CreateSampleCompanyDefinition();
            var sampleEndedActivities = new List<EndedActivity>
            {
                new EndedActivity(DateTime.Now.AddHours(-8), DateTime.Now.AddHours(-4), SystemStateFactory.ApplicationStartedEvent("Sample")),
                new EndedActivity(DateTime.Now.AddHours(-3), DateTime.Now.AddHours(-1), SystemStateFactory.FocusOnApplicationState("Sample"))
            };
            var sampleEndedBreaks = new List<EndedBreak>
            {
                new EndedBreak(Guid.NewGuid(), DateTime.Now.AddHours(-4), DateTime.Now.AddHours(-3), "Lunch Break")
            };

            return CreateBasedOn(sampleWorkdayDefinition, sampleEndedActivities, sampleEndedBreaks);
        }
    }
}
