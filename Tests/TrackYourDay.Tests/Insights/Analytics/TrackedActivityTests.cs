using FluentAssertions;
using TrackYourDay.Core.Insights.Analytics;

namespace TrackYourDay.Tests.Insights.Analytics
{
    public class TrackedActivityTests
    {
        // Test helper class since TrackedActivity is abstract
        private class TestTrackedActivity : TrackedActivity
        {
            private readonly string _description;

            public TestTrackedActivity(DateTime startDate, DateTime endDate, string description = "Test Activity")
                : base(Guid.NewGuid(), startDate, endDate)
            {
                _description = description;
            }

            public override string GetDescription() => _description;
        }

        [Fact]
        public void WhenCreatingTrackedActivity_ThenDurationIsCalculatedCorrectly()
        {
            // Given
            var startDate = new DateTime(2023, 1, 1, 10, 0, 0);
            var endDate = new DateTime(2023, 1, 1, 11, 30, 0);

            // When
            var activity = new TestTrackedActivity(startDate, endDate);

            // Then
            activity.GetDuration().Should().Be(TimeSpan.FromMinutes(90));
        }

        #region GetDate Tests

        [Fact]
        public void WhenGettingDate_ThenReturnsDatePartOfStartDate()
        {
            // Given
            var startDate = new DateTime(2023, 1, 15, 14, 30, 45);
            var endDate = startDate.AddHours(2);
            var activity = new TestTrackedActivity(startDate, endDate);

            // When
            var date = activity.GetDate();

            // Then
            date.Should().Be(new DateOnly(2023, 1, 15));
        }

        [Fact]
        public void GivenActivityStartingAtMidnight_WhenGettingDate_ThenReturnsCorrectDate()
        {
            // Given
            var startDate = new DateTime(2023, 6, 10, 0, 0, 0);
            var endDate = startDate.AddHours(1);
            var activity = new TestTrackedActivity(startDate, endDate);

            // When
            var date = activity.GetDate();

            // Then
            date.Should().Be(new DateOnly(2023, 6, 10));
        }

        [Fact]
        public void GivenActivitySpanningMultipleDays_WhenGettingDate_ThenReturnsStartDate()
        {
            // Given
            var startDate = new DateTime(2023, 3, 20, 23, 30, 0);
            var endDate = new DateTime(2023, 3, 21, 1, 0, 0);
            var activity = new TestTrackedActivity(startDate, endDate);

            // When
            var date = activity.GetDate();

            // Then
            date.Should().Be(new DateOnly(2023, 3, 20));
        }

        #endregion

        #region OverlapsWith Tests

        [Fact]
        public void GivenOverlappingActivities_WhenCheckingOverlapsWith_ThenReturnsTrue()
        {
            // Given
            var activity1 = new TestTrackedActivity(
                new DateTime(2023, 1, 1, 10, 0, 0),
                new DateTime(2023, 1, 1, 12, 0, 0));
            var activity2 = new TestTrackedActivity(
                new DateTime(2023, 1, 1, 11, 0, 0),
                new DateTime(2023, 1, 1, 13, 0, 0));

            // When
            var result = activity1.OverlapsWith(activity2);

            // Then
            result.Should().BeTrue();
        }

        [Fact]
        public void GivenNonOverlappingActivities_WhenCheckingOverlapsWith_ThenReturnsFalse()
        {
            // Given
            var activity1 = new TestTrackedActivity(
                new DateTime(2023, 1, 1, 10, 0, 0),
                new DateTime(2023, 1, 1, 11, 0, 0));
            var activity2 = new TestTrackedActivity(
                new DateTime(2023, 1, 1, 12, 0, 0),
                new DateTime(2023, 1, 1, 13, 0, 0));

            // When
            var result = activity1.OverlapsWith(activity2);

            // Then
            result.Should().BeFalse();
        }

        [Fact]
        public void GivenActivitiesEndingExactlyWhenOtherStarts_WhenCheckingOverlapsWith_ThenReturnsFalse()
        {
            // Given
            var activity1 = new TestTrackedActivity(
                new DateTime(2023, 1, 1, 10, 0, 0),
                new DateTime(2023, 1, 1, 11, 0, 0));
            var activity2 = new TestTrackedActivity(
                new DateTime(2023, 1, 1, 11, 0, 0),
                new DateTime(2023, 1, 1, 12, 0, 0));

            // When
            var result = activity1.OverlapsWith(activity2);

            // Then
            result.Should().BeFalse();
        }

        [Fact]
        public void GivenActivitiesStartingAtExactlySameTime_WhenCheckingOverlapsWith_ThenReturnsTrue()
        {
            // Given
            var activity1 = new TestTrackedActivity(
                new DateTime(2023, 1, 1, 10, 0, 0),
                new DateTime(2023, 1, 1, 11, 0, 0));
            var activity2 = new TestTrackedActivity(
                new DateTime(2023, 1, 1, 10, 0, 0),
                new DateTime(2023, 1, 1, 12, 0, 0));

            // When
            var result = activity1.OverlapsWith(activity2);

            // Then
            result.Should().BeTrue();
        }

        [Fact]
        public void GivenActivitiesEndingAtExactlySameTime_WhenCheckingOverlapsWith_ThenReturnsTrue()
        {
            // Given
            var activity1 = new TestTrackedActivity(
                new DateTime(2023, 1, 1, 10, 0, 0),
                new DateTime(2023, 1, 1, 12, 0, 0));
            var activity2 = new TestTrackedActivity(
                new DateTime(2023, 1, 1, 11, 0, 0),
                new DateTime(2023, 1, 1, 12, 0, 0));

            // When
            var result = activity1.OverlapsWith(activity2);

            // Then
            result.Should().BeTrue();
        }

        [Fact]
        public void GivenOneActivityCompletelyContainsAnother_WhenCheckingOverlapsWith_ThenReturnsTrue()
        {
            // Given
            var activity1 = new TestTrackedActivity(
                new DateTime(2023, 1, 1, 10, 0, 0),
                new DateTime(2023, 1, 1, 14, 0, 0));
            var activity2 = new TestTrackedActivity(
                new DateTime(2023, 1, 1, 11, 0, 0),
                new DateTime(2023, 1, 1, 12, 0, 0));

            // When
            var result1 = activity1.OverlapsWith(activity2);
            var result2 = activity2.OverlapsWith(activity1);

            // Then
            result1.Should().BeTrue();
            result2.Should().BeTrue();
        }

        [Fact]
        public void GivenNullActivity_WhenCheckingOverlapsWith_ThenThrowsArgumentNullException()
        {
            // Given
            var activity = new TestTrackedActivity(
                new DateTime(2023, 1, 1, 10, 0, 0),
                new DateTime(2023, 1, 1, 11, 0, 0));

            // When
            Action act = () => activity.OverlapsWith(null);

            // Then
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("other");
        }

        [Fact]
        public void GivenActivityStartsBeforeOtherEnds_WhenCheckingOverlapsWith_ThenReturnsTrue()
        {
            // Given
            var activity1 = new TestTrackedActivity(
                new DateTime(2023, 1, 1, 10, 0, 0),
                new DateTime(2023, 1, 1, 11, 30, 0));
            var activity2 = new TestTrackedActivity(
                new DateTime(2023, 1, 1, 11, 0, 0),
                new DateTime(2023, 1, 1, 12, 0, 0));

            // When
            var result = activity1.OverlapsWith(activity2);

            // Then
            result.Should().BeTrue();
        }

        [Fact]
        public void GivenActivitiesWithSameStartAndEndTime_WhenCheckingOverlapsWith_ThenReturnsTrue()
        {
            // Given
            var activity1 = new TestTrackedActivity(
                new DateTime(2023, 1, 1, 10, 0, 0),
                new DateTime(2023, 1, 1, 11, 0, 0));
            var activity2 = new TestTrackedActivity(
                new DateTime(2023, 1, 1, 10, 0, 0),
                new DateTime(2023, 1, 1, 11, 0, 0));

            // When
            var result = activity1.OverlapsWith(activity2);

            // Then
            result.Should().BeTrue();
        }

        #endregion

        #region OccurredDuring Tests

        [Fact]
        public void GivenActivityWithinPeriod_WhenCheckingOccurredDuring_ThenReturnsTrue()
        {
            // Given
            var activity = new TestTrackedActivity(
                new DateTime(2023, 1, 1, 11, 0, 0),
                new DateTime(2023, 1, 1, 12, 0, 0));
            var periodStart = new DateTime(2023, 1, 1, 10, 0, 0);
            var periodEnd = new DateTime(2023, 1, 1, 13, 0, 0);

            // When
            var result = activity.OccurredDuring(periodStart, periodEnd);

            // Then
            result.Should().BeTrue();
        }

        [Fact]
        public void GivenActivityOutsidePeriod_WhenCheckingOccurredDuring_ThenReturnsFalse()
        {
            // Given
            var activity = new TestTrackedActivity(
                new DateTime(2023, 1, 1, 14, 0, 0),
                new DateTime(2023, 1, 1, 15, 0, 0));
            var periodStart = new DateTime(2023, 1, 1, 10, 0, 0);
            var periodEnd = new DateTime(2023, 1, 1, 13, 0, 0);

            // When
            var result = activity.OccurredDuring(periodStart, periodEnd);

            // Then
            result.Should().BeFalse();
        }

        [Fact]
        public void GivenActivityStartsAtPeriodStart_WhenCheckingOccurredDuring_ThenReturnsTrue()
        {
            // Given
            var activity = new TestTrackedActivity(
                new DateTime(2023, 1, 1, 10, 0, 0),
                new DateTime(2023, 1, 1, 11, 0, 0));
            var periodStart = new DateTime(2023, 1, 1, 10, 0, 0);
            var periodEnd = new DateTime(2023, 1, 1, 13, 0, 0);

            // When
            var result = activity.OccurredDuring(periodStart, periodEnd);

            // Then
            result.Should().BeTrue();
        }

        [Fact]
        public void GivenActivityEndsAtPeriodEnd_WhenCheckingOccurredDuring_ThenReturnsTrue()
        {
            // Given
            var activity = new TestTrackedActivity(
                new DateTime(2023, 1, 1, 11, 0, 0),
                new DateTime(2023, 1, 1, 13, 0, 0));
            var periodStart = new DateTime(2023, 1, 1, 10, 0, 0);
            var periodEnd = new DateTime(2023, 1, 1, 13, 0, 0);

            // When
            var result = activity.OccurredDuring(periodStart, periodEnd);

            // Then
            result.Should().BeTrue();
        }

        [Fact]
        public void GivenActivityStartsExactlyAtPeriodEnd_WhenCheckingOccurredDuring_ThenReturnsFalse()
        {
            // Given
            var activity = new TestTrackedActivity(
                new DateTime(2023, 1, 1, 13, 0, 0),
                new DateTime(2023, 1, 1, 14, 0, 0));
            var periodStart = new DateTime(2023, 1, 1, 10, 0, 0);
            var periodEnd = new DateTime(2023, 1, 1, 13, 0, 0);

            // When
            var result = activity.OccurredDuring(periodStart, periodEnd);

            // Then
            result.Should().BeFalse();
        }

        [Fact]
        public void GivenActivityEndsExactlyAtPeriodStart_WhenCheckingOccurredDuring_ThenReturnsFalse()
        {
            // Given
            var activity = new TestTrackedActivity(
                new DateTime(2023, 1, 1, 9, 0, 0),
                new DateTime(2023, 1, 1, 10, 0, 0));
            var periodStart = new DateTime(2023, 1, 1, 10, 0, 0);
            var periodEnd = new DateTime(2023, 1, 1, 13, 0, 0);

            // When
            var result = activity.OccurredDuring(periodStart, periodEnd);

            // Then
            result.Should().BeFalse();
        }

        [Fact]
        public void GivenActivitySpansEntirePeriod_WhenCheckingOccurredDuring_ThenReturnsTrue()
        {
            // Given
            var activity = new TestTrackedActivity(
                new DateTime(2023, 1, 1, 9, 0, 0),
                new DateTime(2023, 1, 1, 14, 0, 0));
            var periodStart = new DateTime(2023, 1, 1, 10, 0, 0);
            var periodEnd = new DateTime(2023, 1, 1, 13, 0, 0);

            // When
            var result = activity.OccurredDuring(periodStart, periodEnd);

            // Then
            result.Should().BeTrue();
        }

        [Fact]
        public void GivenActivityPartiallyOverlapsPeriodStart_WhenCheckingOccurredDuring_ThenReturnsTrue()
        {
            // Given
            var activity = new TestTrackedActivity(
                new DateTime(2023, 1, 1, 9, 0, 0),
                new DateTime(2023, 1, 1, 11, 0, 0));
            var periodStart = new DateTime(2023, 1, 1, 10, 0, 0);
            var periodEnd = new DateTime(2023, 1, 1, 13, 0, 0);

            // When
            var result = activity.OccurredDuring(periodStart, periodEnd);

            // Then
            result.Should().BeTrue();
        }

        [Fact]
        public void GivenActivityPartiallyOverlapsPeriodEnd_WhenCheckingOccurredDuring_ThenReturnsTrue()
        {
            // Given
            var activity = new TestTrackedActivity(
                new DateTime(2023, 1, 1, 12, 0, 0),
                new DateTime(2023, 1, 1, 14, 0, 0));
            var periodStart = new DateTime(2023, 1, 1, 10, 0, 0);
            var periodEnd = new DateTime(2023, 1, 1, 13, 0, 0);

            // When
            var result = activity.OccurredDuring(periodStart, periodEnd);

            // Then
            result.Should().BeTrue();
        }

        [Fact]
        public void GivenActivityMatchesExactPeriod_WhenCheckingOccurredDuring_ThenReturnsTrue()
        {
            // Given
            var activity = new TestTrackedActivity(
                new DateTime(2023, 1, 1, 10, 0, 0),
                new DateTime(2023, 1, 1, 13, 0, 0));
            var periodStart = new DateTime(2023, 1, 1, 10, 0, 0);
            var periodEnd = new DateTime(2023, 1, 1, 13, 0, 0);

            // When
            var result = activity.OccurredDuring(periodStart, periodEnd);

            // Then
            result.Should().BeTrue();
        }

        #endregion
    }
}
