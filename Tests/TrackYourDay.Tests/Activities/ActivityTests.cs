using FluentAssertions;
using TrackYourDay.Core.Activities;

namespace TrackYourDay.Tests.Activities
{
    [Trait("Category", "Unit")]
    public class ActivityTests
    {
        [Fact]
        public void WhenActivityIsStarted_ThenItHaveStartDate()
        {
            // Arrange
            var startDate = DateTime.Parse("2008-01-10");

            // Act
            var activity = ActivityFactory.StartedFocusOnApplicatoinActivity(startDate);

            // Assert
            activity.StartDate.Should().Be(startDate);
        }

        [Fact]
        public void WhenActivityIsEnded_ThenItHaveEndDate()
        {
            // Arrange
            var startDate = DateTime.Parse("2008-01-10");
            var activity = ActivityFactory.StartedFocusOnApplicatoinActivity(startDate);
            var endDate = DateTime.Parse("2008-01-10");

            // Act
            var endedActivity = activity.End(endDate);

            // Assert
            endedActivity.EndDate.Should().Be(endDate);
        }

        [Fact]
        public void WhenActivityIsEnded_ThenItHaveEndDateLaterThanStartDate()
        {
            // Arrange
            var startDate = DateTime.Parse("2008-01-10");
            var activity = ActivityFactory.StartedFocusOnApplicatoinActivity(startDate);
            var endDate = DateTime.Parse("2008-01-11");

            // Act
            var endedActivity = activity.End(endDate);

            // Assert
            endedActivity.EndDate.Should().BeAfter(activity.StartDate);
        }

        [Fact]
        public void WhenActivityIsEnded_ThenItsDurationIsTimeBetweenStartDateAndEndDate()
        {
            // Arrange
            var startDate = DateTime.Parse("2008-01-10");
            var activity = ActivityFactory.StartedFocusOnApplicatoinActivity(startDate);
            var endDate = DateTime.Parse("2008-01-20");

            // Act
            var endedActivity = activity.End(endDate);

            // Assert
            endedActivity.GetDuration().Should().Be(TimeSpan.FromDays(10));
        }

        public void WhenInstantActivityIsCreated_ThenItHaveOccuranceDate()
        {
            // Arrange
            var occuranceDate = DateTime.Parse("2008-01-10");

            // Act
            var activity = ActivityFactory.MouseMovedActivity(occuranceDate, null);

            // Assert
            activity.OccuranceDate.Should().Be(occuranceDate);
            activity.GetDuration().Should().Be(TimeSpan.Zero);
        }

    }
}
