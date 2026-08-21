using FluentAssertions;
using TrackYourDay.Core.ApplicationTrackers.GitLab;
using TrackYourDay.Core.Persistence.Specifications;

namespace TrackYourDay.Tests.Persistence
{
    public class DateRangeSpecificationGitLabTests
    {
        [Fact]
        /// <summary>
        /// Checks a bug when specification gets same date as start and end of range, which results in excluding all data expect 00:00:00 for that date.
        /// </summary>
        public void GivenDateRangePointsToSingleDate_WhenCheckingActivityWithinThatRange_ThenItShouldReturnDataForWholeDay()
        {
            // Given
            var singleDay = new DateTime(2000, 1, 15);
            var specification = new DateRangeSpecification<GitLabActivity>(
                DateOnly.FromDateTime(singleDay),
                DateOnly.FromDateTime(singleDay));

            var leftBorderActivity = new GitLabActivity
            {
                UpstreamId = "Activity-1",
                OccurrenceDate = new DateTime(2000, 1, 15, 0, 0, 0),
                Description = "Activity equal to left border"
            };

            var withinBorderActivity = new GitLabActivity
            {
                UpstreamId = "Activity-2",
                OccurrenceDate = new DateTime(2000, 1, 15, 12, 0, 0),
                Description = "Activity within the border"
            };

            var rightBorderActivity = new GitLabActivity
            {
                UpstreamId = "Activity-3",
                OccurrenceDate = new DateTime(2000, 1, 15, 23, 59, 59),
                Description = "Activity equal to right border"
            };

            // When
            var isLeftBorderIncluded = specification.IsSatisfiedBy(leftBorderActivity);
            var isWithinBorderIncluded = specification.IsSatisfiedBy(withinBorderActivity);
            var isRightBorderIncluded = specification.IsSatisfiedBy(rightBorderActivity);

            // Then
            isLeftBorderIncluded.Should().BeTrue("activities equal to the left border should match the specification");
            isWithinBorderIncluded.Should().BeTrue("activities within the border should match the specification");
            isRightBorderIncluded.Should().BeTrue("activities equal to the right border should match the specification");
        }

        [Fact]
        /// <summary>
        /// Checks a bug when specification gets same date as start and end of range, which results in excluding all data expect 00:00:00 for that date.
        /// </summary>
        public void GivenDateRangePointsToSingleDate_WhenCheckingActivityWithinThatRange_ThenSqlRepresentationsCoversWholePeriod()
        {
            // Given
            var singleDay = new DateTime(2000, 1, 15);
            var specification = new DateRangeSpecification<GitLabActivity>(
                DateOnly.FromDateTime(singleDay),
                DateOnly.FromDateTime(singleDay));

            // When
            var sqlParameters = specification.GetSqlParameters();
            var sqlFromParameter = sqlParameters["@fromDate"];
            var sqlToParameter = sqlParameters["@toDate"];
            var sqlWhereClause = specification.GetSqlWhereClause();

            // Then
            sqlFromParameter.Should().Be("2000-01-15");
            sqlToParameter.Should().Be("2000-01-16");
            sqlWhereClause.Should().Be("DATE(json_extract(DataJson, '$.OccurrenceDate')) >= DATE(@fromDate) AND DATE(json_extract(DataJson, '$.OccurrenceDate')) < DATE(@toDate)", "SQL representation should cover the whole day, not just 00:00:00");
        }
    }
}
