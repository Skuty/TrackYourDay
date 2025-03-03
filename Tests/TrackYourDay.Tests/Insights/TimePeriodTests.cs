using FluentAssertions;
using TrackYourDay.Core.Insights.Analytics;

namespace TrackYourDay.Tests.Insights
{
    public class TimePeriodTests
    {
        [Fact]
        public void GivenStartDateIsAfterEndDate_WhenCreatingTimePeriod_ThenThrowsArgumentException()
        {
            // Arrange
            DateTime startDate = new DateTime(2000, 1, 02);
            DateTime endDate = new DateTime(2000, 1, 01);

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new TimePeriod(startDate, endDate));
        }

        [Fact]
        public void WhenCreatingTimePeriod_ThenDurationIsEqualToTimeBetweenStartDateAndEndDate()
        {
            // Arrange
            DateTime startDate = new DateTime(2000, 1, 01, 13, 0, 0);
            DateTime endDate = new DateTime(2000, 1, 01, 14, 30, 0);

            // Act
            var timePeriod = new TimePeriod(startDate, endDate);

            // Assert
            timePeriod.Duration.Should().Be(TimeSpan.FromMinutes(90));
        }

        [Fact]
        public void GivenTwoTimePeriodsOverlapps_WhenCheckingIsTimePeriodOverlappingWithOther_ThenReturnsTrue()
        {
            // Arrange
            var timePeriod1 = new TimePeriod(new DateTime(2000, 1, 01), new DateTime(2000, 1, 02, 13, 0, 0));
            var timePeriod2 = new TimePeriod(new DateTime(2000, 1, 02), new DateTime(2000, 1, 03));

            // Act
            var isOverlapping = timePeriod1.IsOverlappingWith(timePeriod2);

            // Assert
            isOverlapping.Should().Be(true);
        }

        [Fact]
        public void GivenTwoTimePeriodsDoNotOverlapps_WhenCheckingIsTimePeriodOverlappingWithOther_ThenReturnsFalse()
        {
            // Arrange
            var timePeriod1 = new TimePeriod(new DateTime(2000, 1, 01), new DateTime(2000, 1, 02));
            var timePeriod2 = new TimePeriod(new DateTime(2000, 1, 02), new DateTime(2000, 1, 03));

            // Act
            var isOverlapping = timePeriod1.IsOverlappingWith(timePeriod2);

            // Assert
            isOverlapping.Should().Be(false);
        }

        [Fact]
        public void GivenTwoTimePeriodsOverlapps_WhenCalculatingOverlappingDuration_ThenReturnsOverlappingDuration()
        {
            // Arrange
            var timePeriod1 = new TimePeriod(new DateTime(2000, 1, 01), new DateTime(2000, 1, 02, 13, 0, 0));
            var timePeriod2 = new TimePeriod(new DateTime(2000, 1, 02), new DateTime(2000, 1, 03));

            // Act
            var overlappingDuration = timePeriod1.GetOverlappingDuration(timePeriod2);

            // Assert
            overlappingDuration.Should().Be(TimeSpan.FromHours(13));
        }

        [Fact]
        public void GivenTwoNonOverlappingTimePeriods_WhenCalculatingOverlappingDuration_ThenReturnsZero()
        {
            // Arrange
            var timePeriod1 = new TimePeriod(new DateTime(2000, 1, 01), new DateTime(2000, 1, 02));
            var timePeriod2 = new TimePeriod(new DateTime(2000, 1, 02), new DateTime(2000, 1, 03));

            // Act
            var overlappingDuration = timePeriod1.GetOverlappingDuration(timePeriod2);

            // Assert
            overlappingDuration.Should().Be(TimeSpan.Zero);
        }
    }
}

