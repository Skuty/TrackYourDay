using FluentAssertions;
using TrackYourDay.Core.Old.Activities;

namespace TrackYourDay.Tests.Old.Activities
{
    /// <summary>
    /// Tests are aware of the implementation of ActivityTracer.
    /// </summary>
    public class ActivitiesSummaryTests
    {
        [Fact]
        public void GivenThereIsOneOrLessActivity_WhenGettingTimeOfAllActivities_ThenSummedTimeOfActivitiesIsEqualToZero()
        {
            // Arrange
            var activities = new List<ActivityEvent>()
            {
                ActivityEvent.CreateEvent(DateTime.Parse("2000-01-01 12:00:00"), new FocusOnApplicationActivity(string.Empty))
            };
            var sut = new ActivitiesSummary(activities);

            // Act
            var result = sut.GetTimeOfAllActivities();

            // Assert
            result.Should().Be(TimeSpan.Zero);
        }

        // 2 activities
        // 3 activities
        // 5 activities
        [Fact]
        public void GivenThereIsMoreThanOneActivity__WhenGettingTimeOfAllActivities_ThenTimeBetweenFirstAndLastIsReturned()
        {
            // Arrange
            var activities = new List<ActivityEvent>()
            {
                ActivityEvent.CreateEvent(DateTime.Parse("2000-01-01 12:00:00"), new FocusOnApplicationActivity(string.Empty)),
                ActivityEvent.CreateEvent(DateTime.Parse("2000-01-01 12:30:00"), new SystemLockedActivity()),
                ActivityEvent.CreateEvent(DateTime.Parse("2000-01-01 12:45:00"), new FocusOnApplicationActivity(string.Empty))
            };
            var sut = new ActivitiesSummary(activities);

            // Act
            var result = sut.GetTimeOfAllActivities();

            // Assert
            result.Should().Be(TimeSpan.FromMinutes(45));
        }

        // SystemLocked
        // FocusOnApplication
        [Fact]
        public void WhenSpecificActivityTimeIsCalculated_ThenSummedTimeBetweenEachSpecificActivityAndNextDifferentActivityIsReturned()
        {
            // Arrange
            var activities = new List<ActivityEvent>()
            {
                ActivityEvent.CreateEvent(DateTime.Parse("2000-01-01 12:00:00"), new FocusOnApplicationActivity(string.Empty)),
                ActivityEvent.CreateEvent(DateTime.Parse("2000-01-01 12:30:00"), new SystemLockedActivity()),
                ActivityEvent.CreateEvent(DateTime.Parse("2000-01-01 12:50:00"), new FocusOnApplicationActivity(string.Empty)),
                ActivityEvent.CreateEvent(DateTime.Parse("2000-01-01 12:55:00"), new SystemLockedActivity()),
                ActivityEvent.CreateEvent(DateTime.Parse("2000-01-01 13:00:00"), new FocusOnApplicationActivity(string.Empty))
            };
            var sut = new ActivitiesSummary(activities);

            // Act
            var result = sut.GetTimeOfSpecificActivity<FocusOnApplicationActivity>();

            // Assert
            result.Should().Be(TimeSpan.FromMinutes(35));
        }

        [Fact]
        public void WhenGettingListOfActivities_ThenDistinctListOfActivitiesIsReturned()
        {
            // Arrange
            var activities = new List<ActivityEvent>()
            {
                ActivityEvent.CreateEvent(DateTime.Parse("2000-01-01 12:00:00"), new FocusOnApplicationActivity(string.Empty)),
                ActivityEvent.CreateEvent(DateTime.Parse("2000-01-01 12:30:00"), new SystemLockedActivity()),
                ActivityEvent.CreateEvent(DateTime.Parse("2000-01-01 12:50:00"), new FocusOnApplicationActivity(string.Empty)),
                ActivityEvent.CreateEvent(DateTime.Parse("2000-01-01 12:55:00"), new SystemLockedActivity()),
                ActivityEvent.CreateEvent(DateTime.Parse("2000-01-01 13:00:00"), new FocusOnApplicationActivity(string.Empty))
            };

            var sut = new ActivitiesSummary(activities);

            // Act
            var result = sut.GetListOfActivities();

            // Arrange
            result.Count().Should().Be(2);

        }
    }
}
