using TrackYourDay.Core.SystemTrackers;

namespace TrackYourDay.Core.Persistence.Specifications
{
    /// <summary>
    /// Specification for filtering EndedActivity by a specific date.
    /// Queries activities where StartDate matches the specified date.
    /// </summary>
    public class ActivityByDateSpecification : ISpecification<EndedActivity>
    {
        private readonly DateOnly date;

        public ActivityByDateSpecification(DateOnly date)
        {
            this.date = date;
        }

        public string GetSqlWhereClause()
        {
            return "date(json_extract(DataJson, '$.StartDate')) = @date";
        }

        public Dictionary<string, object> GetSqlParameters()
        {
            return new Dictionary<string, object>
            {
                { "@date", date.ToString("yyyy-MM-dd") }
            };
        }

        public bool IsSatisfiedBy(EndedActivity entity)
        {
            return DateOnly.FromDateTime(entity.StartDate) == date;
        }
    }

    /// <summary>
    /// Specification for filtering EndedActivity by a date range.
    /// Queries activities where StartDate is between the specified dates (inclusive).
    /// </summary>
    public class ActivityByDateRangeSpecification : ISpecification<EndedActivity>
    {
        private readonly DateOnly startDate;
        private readonly DateOnly endDate;

        public ActivityByDateRangeSpecification(DateOnly startDate, DateOnly endDate)
        {
            this.startDate = startDate;
            this.endDate = endDate;
        }

        public string GetSqlWhereClause()
        {
            return @"date(json_extract(DataJson, '$.StartDate')) >= @startDate 
                     AND date(json_extract(DataJson, '$.StartDate')) <= @endDate";
        }

        public Dictionary<string, object> GetSqlParameters()
        {
            return new Dictionary<string, object>
            {
                { "@startDate", startDate.ToString("yyyy-MM-dd") },
                { "@endDate", endDate.ToString("yyyy-MM-dd") }
            };
        }

        public bool IsSatisfiedBy(EndedActivity entity)
        {
            var entityDate = DateOnly.FromDateTime(entity.StartDate);
            return entityDate >= startDate && entityDate <= endDate;
        }
    }
}
