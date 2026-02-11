using FluentAssertions;
using TrackYourDay.Core.Insights.Analytics;

namespace TrackYourDay.Tests.Insights
{
    public class GropuedActivityTests
    {
        [Fact]
        public void GivenDurationWasNotExtendedByTimePeriod_WhenDurationIsExtendedByTimePeriod_ThenDurrationIsExtendedByTimeOfPeriod()
        {
            // Given
            var groupedActivity = GroupedActivity.CreateEmptyForDate(new DateOnly(2000, 01, 01));
            var activityStartDate = new DateTime(2000, 01, 01, 00, 00, 00);
            var activityEndDate = new DateTime(2000, 01, 01, 01, 00, 00);
            var periodToInclude = TimePeriod.CreateFrom(activityStartDate, activityEndDate);

            // When
            groupedActivity.Include(Guid.NewGuid(), periodToInclude);

            // Then
            groupedActivity.Duration.Should().Be(TimeSpan.FromHours(1));
        }

        [Fact]
        public void GivenDurationWasExtendedByTimePeriod_WhenDurationIsExtendedByThatSameTimePeriod_ThenDurrationIsNotChanged()
        {
            // Given
            var groupedActivity = GroupedActivity.CreateEmptyForDate(new DateOnly(2000, 01, 01));
            var activityStartDate = new DateTime(2000, 01, 01, 00, 00, 00);
            var activityEndDate = new DateTime(2000, 01, 01, 01, 00, 00);
            var periodToInclude = TimePeriod.CreateFrom(activityStartDate, activityEndDate);
            var eventGuid = Guid.NewGuid();
            groupedActivity.Include(eventGuid, periodToInclude);

            // When
            groupedActivity.Include(eventGuid, periodToInclude);

            // Then
            groupedActivity.Duration.Should().Be(TimeSpan.FromHours(1));
        }

        [Fact]
        public void GivenDurationWasNotReducedByTimePeriod_WhenDurationIsReducedByFullyOverlappingTimePeriod_ThenDurrationIsReducedByFullTimeOfTimePeriod()
        {
            // Given
            var groupedActivity = GroupedActivity.CreateEmptyForDate(new DateOnly(2000, 01, 01));
            var activityStartDate = new DateTime(2000, 01, 01, 00, 00, 00);
            var activityEndDate = new DateTime(2000, 01, 01, 01, 00, 00);
            var periodToInclude = TimePeriod.CreateFrom(activityStartDate, activityEndDate);
            var periodToExclude = TimePeriod.CreateFrom(activityStartDate, activityStartDate.AddMinutes(30));
            groupedActivity.Include(Guid.NewGuid(), periodToInclude);

            // When
            groupedActivity.ReduceBy(Guid.NewGuid(), periodToExclude);

            // Then
            groupedActivity.Duration.Should().Be(TimeSpan.FromMinutes(30));
        }

        [Fact]
        public void GivenDurationWasNotReducedByTimePeriod_WhenDurationIsReducedByPartiallyOverlappingTimePeriod_ThenDurrationIsReducedByTimeOfOverlappingTimePeriod()
        {
            // Given
            var groupedActivity = GroupedActivity.CreateEmptyForDate(new DateOnly(2000, 01, 01));
            var activityStartDate = new DateTime(2000, 01, 01, 00, 00, 00);
            var activityEndDate = new DateTime(2000, 01, 01, 01, 00, 00);
            var periodToInclude = TimePeriod.CreateFrom(activityStartDate, activityEndDate);
            var periodToExcelude = TimePeriod.CreateFrom(activityEndDate.AddMinutes(-15), activityEndDate.AddMinutes(15));
            groupedActivity.Include(Guid.NewGuid(), periodToInclude);

            // When
            groupedActivity.ReduceBy(Guid.NewGuid(), periodToExcelude);

            // Then
            groupedActivity.Duration.Should().Be(TimeSpan.FromMinutes(45));
        }

        [Fact]
        public void GivenDurationWasReducedByTimePeriod_WhenDurationIsReducedByThatSameTimePeriod_ThenDurrationIsNotChanged()
        {
            // Given
            var groupedActivity = GroupedActivity.CreateEmptyForDate(new DateOnly(2000, 01, 01));
            var activityStartDate = new DateTime(2000, 01, 01, 00, 00, 00);
            var activityEndDate = new DateTime(2000, 01, 01, 01, 00, 00);
            var periodToInclude = TimePeriod.CreateFrom(activityStartDate, activityEndDate);
            var periodToExclude = TimePeriod.CreateFrom(activityEndDate.AddMinutes(-30), activityEndDate);
            groupedActivity.Include(Guid.NewGuid(), periodToInclude);
            var eventGuid = Guid.NewGuid();
            groupedActivity.ReduceBy(eventGuid, periodToExclude);

            // When
            groupedActivity.ReduceBy(eventGuid, periodToExclude);

            // Then
            groupedActivity.Duration.Should().Be(TimeSpan.FromMinutes(30));
        }

        [Fact]
        public void GivenConcurrentActivities_WhenCalculatingDuration_ThenReturnsWallClockTime()
        {
            // Given
            var groupedActivity = GroupedActivity.CreateEmptyForDate(new DateOnly(2000, 01, 01));
            var startDate = new DateTime(2000, 01, 01, 09, 00, 00);
            var endDate = new DateTime(2000, 01, 01, 10, 00, 00);
            var period1 = new TimePeriod(startDate, endDate);
            var period2 = new TimePeriod(startDate, endDate);

            // When
            groupedActivity.Include(Guid.NewGuid(), period1);
            groupedActivity.Include(Guid.NewGuid(), period2);

            // Then
            groupedActivity.Duration.Should().Be(TimeSpan.FromHours(1));
        }

        [Fact]
        public void GivenSequentialActivities_WhenCalculatingDuration_ThenSumsDurations()
        {
            // Given
            var groupedActivity = GroupedActivity.CreateEmptyForDate(new DateOnly(2000, 01, 01));
            var period1 = new TimePeriod(new DateTime(2000, 01, 01, 09, 00, 00), new DateTime(2000, 01, 01, 10, 00, 00));
            var period2 = new TimePeriod(new DateTime(2000, 01, 01, 10, 00, 00), new DateTime(2000, 01, 01, 11, 00, 00));

            // When
            groupedActivity.Include(Guid.NewGuid(), period1);
            groupedActivity.Include(Guid.NewGuid(), period2);

            // Then
            groupedActivity.Duration.Should().Be(TimeSpan.FromHours(2));
        }

        [Fact]
        public void GivenOverlappingActivitiesWithBreak_WhenReducing_ThenSubtractsBreakOnce()
        {
            // Given
            var groupedActivity = GroupedActivity.CreateEmptyForDate(new DateOnly(2000, 01, 01));
            var startDate = new DateTime(2000, 01, 01, 09, 00, 00);
            var endDate = new DateTime(2000, 01, 01, 10, 00, 00);
            var breakStart = new DateTime(2000, 01, 01, 09, 30, 00);
            var breakEnd = new DateTime(2000, 01, 01, 09, 45, 00);

            // When
            groupedActivity.Include(Guid.NewGuid(), new TimePeriod(startDate, endDate));
            groupedActivity.Include(Guid.NewGuid(), new TimePeriod(startDate, endDate));
            groupedActivity.ReduceBy(Guid.NewGuid(), new TimePeriod(breakStart, breakEnd));

            // Then
            groupedActivity.Duration.Should().Be(TimeSpan.FromMinutes(45));
        }

        [Fact]
        public void GivenPartiallyOverlappingActivities_WhenCalculatingDuration_ThenMergesCorrectly()
        {
            // Given
            var groupedActivity = GroupedActivity.CreateEmptyForDate(new DateOnly(2000, 01, 01));
            var period1 = new TimePeriod(new DateTime(2000, 01, 01, 09, 00, 00), new DateTime(2000, 01, 01, 10, 00, 00));
            var period2 = new TimePeriod(new DateTime(2000, 01, 01, 09, 30, 00), new DateTime(2000, 01, 01, 11, 00, 00));

            // When
            groupedActivity.Include(Guid.NewGuid(), period1);
            groupedActivity.Include(Guid.NewGuid(), period2);

            // Then
            groupedActivity.Duration.Should().Be(TimeSpan.FromHours(2));
        }

        [Fact]
        public void GivenMultipleNonOverlappingBreaks_WhenReducing_ThenDurationIsCorrect()
        {
            // Given
            var groupedActivity = GroupedActivity.CreateEmptyForDate(new DateOnly(2000, 01, 01));
            var activityPeriod = new TimePeriod(new DateTime(2000, 01, 01, 09, 00, 00), new DateTime(2000, 01, 01, 11, 00, 00));
            var break1 = new TimePeriod(new DateTime(2000, 01, 01, 09, 15, 00), new DateTime(2000, 01, 01, 09, 30, 00));
            var break2 = new TimePeriod(new DateTime(2000, 01, 01, 10, 00, 00), new DateTime(2000, 01, 01, 10, 15, 00));

            // When
            groupedActivity.Include(Guid.NewGuid(), activityPeriod);
            groupedActivity.ReduceBy(Guid.NewGuid(), break1);
            groupedActivity.ReduceBy(Guid.NewGuid(), break2);

            // Then
            groupedActivity.Duration.Should().Be(TimeSpan.FromMinutes(90));
        }

        [Fact]
        public void GivenGetIncludedOccurrences_WhenCalled_ThenReturnsCorrectGuidPeriodMapping()
        {
            // Given
            var groupedActivity = GroupedActivity.CreateEmptyForDate(new DateOnly(2000, 01, 01));
            var guid1 = Guid.NewGuid();
            var guid2 = Guid.NewGuid();
            var breakGuid = Guid.NewGuid();
            var period1 = new TimePeriod(new DateTime(2000, 01, 01, 09, 00, 00), new DateTime(2000, 01, 01, 10, 00, 00));
            var period2 = new TimePeriod(new DateTime(2000, 01, 01, 10, 30, 00), new DateTime(2000, 01, 01, 11, 00, 00));
            var breakPeriod = new TimePeriod(new DateTime(2000, 01, 01, 10, 00, 00), new DateTime(2000, 01, 01, 10, 30, 00));

            // When
            groupedActivity.Include(guid1, period1);
            groupedActivity.Include(guid2, period2);
            groupedActivity.ReduceBy(breakGuid, breakPeriod);
            var occurrences = groupedActivity.GetIncludedOccurrences();

            // Then
            occurrences.Should().HaveCount(2);
            occurrences[0].EventId.Should().Be(guid1);
            occurrences[0].Period.Should().Be(period1);
            occurrences[1].EventId.Should().Be(guid2);
            occurrences[1].Period.Should().Be(period2);
        }

        [Fact]
        public void GivenGetExcludedOccurrences_WhenCalled_ThenReturnsBreaks()
        {
            // Given
            var groupedActivity = GroupedActivity.CreateEmptyForDate(new DateOnly(2000, 01, 01));
            var breakGuid = Guid.NewGuid();
            var breakPeriod = new TimePeriod(new DateTime(2000, 01, 01, 10, 00, 00), new DateTime(2000, 01, 01, 10, 30, 00));

            // When
            groupedActivity.Include(Guid.NewGuid(), new TimePeriod(new DateTime(2000, 01, 01, 09, 00, 00), new DateTime(2000, 01, 01, 11, 00, 00)));
            groupedActivity.ReduceBy(breakGuid, breakPeriod);
            var excluded = groupedActivity.GetExcludedOccurrences();

            // Then
            excluded.Should().HaveCount(1);
            excluded[0].EventId.Should().Be(breakGuid);
            excluded[0].Period.Should().Be(breakPeriod);
        }
    }
}
