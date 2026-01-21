using TrackYourDay.Core.ApplicationTrackers.Shared;

namespace TrackYourDay.Core.Persistence.Specifications
{
    /// <summary>
    /// Specification for filtering entities by occurrence date range.
    /// Works with types implementing IHasOccurrenceDate.
    /// </summary>
    public sealed class DateRangeSpecification<T> : ISpecification<T> where T : class, IHasOccurrenceDate
    {
        private readonly DateOnly _fromDate;
        private readonly DateOnly _toDate;

        public DateRangeSpecification(DateOnly fromDate, DateOnly toDate)
        {
            _fromDate = fromDate;
            _toDate = toDate;
        }

        public string GetSqlWhereClause()
        {
            return "DATE(json_extract(DataJson, '$.OccurrenceDate')) >= DATE(@fromDate) AND DATE(json_extract(DataJson, '$.OccurrenceDate')) <= DATE(@toDate)";
        }

        public Dictionary<string, object> GetSqlParameters()
        {
            return new Dictionary<string, object>
            {
                { "@fromDate", _fromDate.ToString("yyyy-MM-dd") },
                { "@toDate", _toDate.ToString("yyyy-MM-dd") }
            };
        }

        public bool IsSatisfiedBy(T entity)
        {
            var occurrenceDate = DateOnly.FromDateTime(entity.OccurrenceDate);
            return occurrenceDate >= _fromDate && occurrenceDate <= _toDate;
        }
    }
}
