using TrackYourDay.Core.ApplicationTrackers.GitLab;

namespace TrackYourDay.Core.Persistence.Specifications
{
    /// <summary>
    /// Specification for filtering GitLabActivity by a specific date.
    /// Queries activities where OccuranceDate matches the specified date.
    /// </summary>
    public class GitLabActivityByDateSpecification : ISpecification<GitLabActivity>
    {
        private readonly DateOnly date;

        public GitLabActivityByDateSpecification(DateOnly date)
        {
            this.date = date;
        }

        public string GetSqlWhereClause()
        {
            return "date(json_extract(DataJson, '$.OccuranceDate')) = @date";
        }

        public Dictionary<string, object> GetSqlParameters()
        {
            return new Dictionary<string, object>
            {
                { "@date", date.ToString("yyyy-MM-dd") }
            };
        }

        public bool IsSatisfiedBy(GitLabActivity entity)
        {
            return DateOnly.FromDateTime(entity.OccuranceDate) == date;
        }
    }

    /// <summary>
    /// Specification for filtering GitLabActivity by a date range.
    /// Queries activities where OccuranceDate is between the specified dates (inclusive).
    /// </summary>
    public class GitLabActivityByDateRangeSpecification : ISpecification<GitLabActivity>
    {
        private readonly DateOnly startDate;
        private readonly DateOnly endDate;

        public GitLabActivityByDateRangeSpecification(DateOnly startDate, DateOnly endDate)
        {
            this.startDate = startDate;
            this.endDate = endDate;
        }

        public string GetSqlWhereClause()
        {
            return @"date(json_extract(DataJson, '$.OccuranceDate')) >= @startDate 
                     AND date(json_extract(DataJson, '$.OccuranceDate')) <= @endDate";
        }

        public Dictionary<string, object> GetSqlParameters()
        {
            return new Dictionary<string, object>
            {
                { "@startDate", startDate.ToString("yyyy-MM-dd") },
                { "@endDate", endDate.ToString("yyyy-MM-dd") }
            };
        }

        public bool IsSatisfiedBy(GitLabActivity entity)
        {
            var entityDate = DateOnly.FromDateTime(entity.OccuranceDate);
            return entityDate >= startDate && entityDate <= endDate;
        }
    }
}
