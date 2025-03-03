namespace TrackYourDay.Core.Insights.Analytics
{
    public record class TimePeriod
    {
        public DateTime StartDate { get; init; }
        public DateTime EndDate { get; init; }
        public TimeSpan Duration { get; init; }

        public TimePeriod(DateTime startDate, DateTime endDate)
        {
            if (startDate > endDate)
            {
                throw new ArgumentException($"{nameof(startDate)} have to be earlier or equal to {nameof(endDate)}", nameof(startDate));
            }

            StartDate = startDate;
            EndDate = endDate;
            Duration = EndDate - StartDate;
        }

        public bool IsOverlappingWith(TimePeriod timePeriod)
        {
            return StartDate < timePeriod.EndDate && timePeriod.StartDate < EndDate;

        }

        public TimeSpan GetOverlappingDuration(TimePeriod timePeriod)
        {
            if (!IsOverlappingWith(timePeriod))
            {
                return TimeSpan.Zero;
            }

            var overlappingStartDate = new DateTime(Math.Max(StartDate.Ticks, timePeriod.StartDate.Ticks));

            var overlappingEndDate = new DateTime(Math.Min(EndDate.Ticks, timePeriod.EndDate.Ticks));

            return overlappingEndDate - overlappingStartDate;
        }

        public static TimePeriod CreateFrom(DateTime startDate, DateTime endDate)
        {
            return new TimePeriod(startDate, endDate);
        }
    }
}