using FluentAssertions;
using TrackYourDay.Core.Activities;
using TrackYourDay.Core.Analytics;
using TrackYourDay.Core.Breaks;

namespace TrackYourDay.Tests.Analytics
{
    public class GropuedActivityTests
    {
        [Fact]
        public void GivenDurationWasNotExtendedByActivity_WhenDurationIsExtendedByActivityDuration_ThenDurrationIsExtendedByTimeOfActivity()
        {
            // Given
            var groupedActivity = GropuedActivity.CreateForDate(new DateOnly(2000, 01, 01));
            var activityStartDate = new DateTime(2000, 01, 01, 00, 00, 00);
            var activityEndDate = new DateTime(2000, 01, 01, 01, 00, 00);
            var endedActivity = ActivityFactory.EndedFocusOnApplicationActivity(activityStartDate, activityEndDate);

            // When
            groupedActivity.Include(endedActivity);

            // Then
            groupedActivity.Duration.Should().Be(TimeSpan.FromHours(1));
        }

        [Fact]
        public void GivenDurationWasExtendedByActivity_WhenDurationIsExtendedByActivityDuration_ThenDurrationIsNotChanged()
        {
            // Given
            var groupedActivity = GropuedActivity.CreateForDate(new DateOnly(2000, 01, 01));
            var activityStartDate = new DateTime(2000, 01, 01, 00, 00, 00);
            var activityEndDate = new DateTime(2000, 01, 01, 01, 00, 00);
            var endedActivity = ActivityFactory.EndedFocusOnApplicationActivity(activityStartDate, activityEndDate);
            groupedActivity.Include(endedActivity);

            // When
            groupedActivity.Include(endedActivity);

            // Then
            groupedActivity.Duration.Should().Be(TimeSpan.FromHours(1));
        }

        [Fact]
        public void GivenDurationWasNotReducedByBreak_WhenDurationIsReducedByFullyOverlappingBreak_ThenDurrationIsReducedByFullTimeOfBreak()
        {
            // Given
            var groupedActivity = GropuedActivity.CreateForDate(new DateOnly(2000, 01, 01));
            var activityStartDate = new DateTime(2000, 01, 01, 00, 00, 00);
            var activityEndDate = new DateTime(2000, 01, 01, 01, 00, 00);
            var endedActivity = ActivityFactory.EndedFocusOnApplicationActivity(activityStartDate, activityEndDate);
            var fullyOverlappingBreak = EndedBreak.CreateSampleForDate(activityStartDate, activityStartDate.AddMinutes(30));
            groupedActivity.Include(endedActivity);

            // When
            groupedActivity.ReduceBy(fullyOverlappingBreak);

            // Then
            groupedActivity.Duration.Should().Be(TimeSpan.FromMinutes(30));
        }

        [Fact]
        public void GivenDurationWasNotReducedByBreak_WhenDurationIsReducedByPartiallyOverlappingBreak_ThenDurrationIsReducedByTimeOfPartiallyOverlappingBreak()
        {
            // Given
            var groupedActivity = GropuedActivity.CreateForDate(new DateOnly(2000, 01, 01));
            var activityStartDate = new DateTime(2000, 01, 01, 00, 00, 00);
            var activityEndDate = new DateTime(2000, 01, 01, 01, 00, 00);
            var endedActivity = ActivityFactory.EndedFocusOnApplicationActivity(activityStartDate, activityEndDate);
            var fullyOverlappingBreak = EndedBreak.CreateSampleForDate(activityStartDate.AddMinutes(45), activityStartDate.AddMinutes(75));
            groupedActivity.Include(endedActivity);

            // When
            groupedActivity.ReduceBy(fullyOverlappingBreak);

            // Then
            groupedActivity.Duration.Should().Be(TimeSpan.FromMinutes(15));
        }

        [Fact]
        public void GivenDurationWasReducedByBreak_WhenDurationIsReducedByBreak_ThenDurrationIsNotChanged()
        {
            // Given
            var groupedActivity = GropuedActivity.CreateForDate(new DateOnly(2000, 01, 01));
            var activityStartDate = new DateTime(2000, 01, 01, 00, 00, 00);
            var activityEndDate = new DateTime(2000, 01, 01, 01, 00, 00);
            var endedActivity = ActivityFactory.EndedFocusOnApplicationActivity(activityStartDate, activityEndDate);
            var endedBreak = EndedBreak.CreateSampleForDate(activityStartDate, activityStartDate.AddMinutes(30));
            groupedActivity.Include(endedActivity);
            groupedActivity.ReduceBy(endedBreak);

            // When
            groupedActivity.ReduceBy(endedBreak);

            // Then
            groupedActivity.Duration.Should().Be(TimeSpan.FromMinutes(30));
        }
    }
}
