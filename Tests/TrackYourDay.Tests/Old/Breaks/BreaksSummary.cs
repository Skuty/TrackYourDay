﻿
using TrackYourDay.Core.ApplicationTrackers.Breaks;

namespace TrackYourDay.Tests.Old.Breaks
{
    public class BreaksSummary
    {
        private List<EndedBreak> breaks;

        public BreaksSummary(List<EndedBreak> breaks)
        {
            this.breaks = breaks;
        }

        public TimeSpan GetTimeOfAllBreaks()
        {
            var timeOfAllBreaks = TimeSpan.Zero;

            foreach (var @break in breaks)
            {
                timeOfAllBreaks += @break.BreakDuration;
            }

            return timeOfAllBreaks;
        }

        public int GetCountOfAllBreaks()
        {
            return breaks.Count;
        }
    }
}