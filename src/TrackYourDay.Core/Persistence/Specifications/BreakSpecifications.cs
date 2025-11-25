using TrackYourDay.Core.ApplicationTrackers.Breaks;

namespace TrackYourDay.Core.Persistence.Specifications
{
    /// <summary>
    /// Specification for filtering EndedBreak by a specific date.
    /// Queries breaks where BreakStartedAt matches the specified date.
    /// </summary>
    public class BreakByDateSpecification : ISpecification<EndedBreak>
    {
        private readonly DateOnly date;

        public BreakByDateSpecification(DateOnly date)
        {
            this.date = date;
        }

        public string GetSqlWhereClause()
        {
            return "date(json_extract(DataJson, '$.BreakStartedAt')) = @date";
        }

        public Dictionary<string, object> GetSqlParameters()
        {
            return new Dictionary<string, object>
            {
                { "@date", date.ToString("yyyy-MM-dd") }
            };
        }

        public bool IsSatisfiedBy(EndedBreak entity)
        {
            return DateOnly.FromDateTime(entity.BreakStartedAt) == date;
        }
    }

    /// <summary>
    /// Specification for filtering EndedBreak by a date range.
    /// Queries breaks where BreakStartedAt is between the specified dates (inclusive).
    /// </summary>
    public class BreakByDateRangeSpecification : ISpecification<EndedBreak>
    {
        private readonly DateOnly startDate;
        private readonly DateOnly endDate;

        public BreakByDateRangeSpecification(DateOnly startDate, DateOnly endDate)
        {
            this.startDate = startDate;
            this.endDate = endDate;
        }

        public string GetSqlWhereClause()
        {
            return @"date(json_extract(DataJson, '$.BreakStartedAt')) >= @startDate 
                     AND date(json_extract(DataJson, '$.BreakStartedAt')) <= @endDate";
        }

        public Dictionary<string, object> GetSqlParameters()
        {
            return new Dictionary<string, object>
            {
                { "@startDate", startDate.ToString("yyyy-MM-dd") },
                { "@endDate", endDate.ToString("yyyy-MM-dd") }
            };
        }

        public bool IsSatisfiedBy(EndedBreak entity)
        {
            var entityDate = DateOnly.FromDateTime(entity.BreakStartedAt);
            return entityDate >= startDate && entityDate <= endDate;
        }
    }
}
