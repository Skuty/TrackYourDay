namespace TrackYourDay.Core.Analytics
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

            this.StartDate = startDate;
            this.EndDate = endDate;
            this.Duration = EndDate - StartDate;
        }

        public bool IsOverlappingWith(TimePeriod timePeriod)
        {
            return this.StartDate < timePeriod.EndDate && timePeriod.StartDate < this.EndDate;

        }

        public TimeSpan GetOverlappingDuration(TimePeriod timePeriod)
        {
            if (!this.IsOverlappingWith(timePeriod))
            {
                return TimeSpan.Zero;
            }

            var overlappingStartDate = new DateTime(Math.Max(this.StartDate.Ticks, timePeriod.StartDate.Ticks));

            var overlappingEndDate = new DateTime(Math.Min(this.EndDate.Ticks, timePeriod.EndDate.Ticks));

            return overlappingEndDate - overlappingStartDate;
        }
    }
}