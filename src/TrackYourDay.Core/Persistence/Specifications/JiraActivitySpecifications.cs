using TrackYourDay.Core.ApplicationTrackers.Jira;

namespace TrackYourDay.Core.Persistence.Specifications
{
    /// <summary>
    /// Specification for filtering JiraActivity by a specific date.
    /// Queries activities where OccurrenceDate matches the specified date.
    /// </summary>
    public class JiraActivityByDateSpecification : ISpecification<JiraActivity>
    {
        private readonly DateOnly date;

        public JiraActivityByDateSpecification(DateOnly date)
        {
            this.date = date;
        }

        public string GetSqlWhereClause()
        {
            return "date(json_extract(DataJson, '$.OccurrenceDate')) = @date";
        }

        public Dictionary<string, object> GetSqlParameters()
        {
            return new Dictionary<string, object>
            {
                { "@date", date.ToString("yyyy-MM-dd") }
            };
        }

        public bool IsSatisfiedBy(JiraActivity entity)
        {
            return DateOnly.FromDateTime(entity.OccurrenceDate) == date;
        }
    }

    /// <summary>
    /// Specification for filtering JiraActivity by a date range.
    /// Queries activities where OccurrenceDate is between the specified dates (inclusive).
    /// </summary>
    public class JiraActivityByDateRangeSpecification : ISpecification<JiraActivity>
    {
        private readonly DateOnly startDate;
        private readonly DateOnly endDate;

        public JiraActivityByDateRangeSpecification(DateOnly startDate, DateOnly endDate)
        {
            this.startDate = startDate;
            this.endDate = endDate;
        }

        public string GetSqlWhereClause()
        {
            return @"date(json_extract(DataJson, '$.OccurrenceDate')) >= @startDate 
                     AND date(json_extract(DataJson, '$.OccurrenceDate')) <= @endDate";
        }

        public Dictionary<string, object> GetSqlParameters()
        {
            return new Dictionary<string, object>
            {
                { "@startDate", startDate.ToString("yyyy-MM-dd") },
                { "@endDate", endDate.ToString("yyyy-MM-dd") }
            };
        }

        public bool IsSatisfiedBy(JiraActivity entity)
        {
            var entityDate = DateOnly.FromDateTime(entity.OccurrenceDate);
            return entityDate >= startDate && entityDate <= endDate;
        }
    }
}
