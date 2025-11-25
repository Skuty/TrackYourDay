using TrackYourDay.Core.ApplicationTrackers.MsTeams;

namespace TrackYourDay.Core.Persistence.Specifications
{
    /// <summary>
    /// Specification for filtering EndedMeeting by a specific date.
    /// Queries meetings where StartDate matches the specified date.
    /// </summary>
    public class MeetingByDateSpecification : ISpecification<EndedMeeting>
    {
        private readonly DateOnly date;

        public MeetingByDateSpecification(DateOnly date)
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

        public bool IsSatisfiedBy(EndedMeeting entity)
        {
            return DateOnly.FromDateTime(entity.StartDate) == date;
        }
    }

    /// <summary>
    /// Specification for filtering EndedMeeting by a date range.
    /// Queries meetings where StartDate is between the specified dates (inclusive).
    /// </summary>
    public class MeetingByDateRangeSpecification : ISpecification<EndedMeeting>
    {
        private readonly DateOnly startDate;
        private readonly DateOnly endDate;

        public MeetingByDateRangeSpecification(DateOnly startDate, DateOnly endDate)
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

        public bool IsSatisfiedBy(EndedMeeting entity)
        {
            var entityDate = DateOnly.FromDateTime(entity.StartDate);
            return entityDate >= startDate && entityDate <= endDate;
        }
    }
}
