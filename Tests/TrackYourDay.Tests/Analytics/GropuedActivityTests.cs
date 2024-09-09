using FluentAssertions;
using TrackYourDay.Core.Analytics;

namespace TrackYourDay.Tests.Analytics
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
    }
}
