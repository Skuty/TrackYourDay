using FluentAssertions;
using TrackYourDay.Core;
using TrackYourDay.Core.Activities;
using TrackYourDay.Core.Breaks;

namespace TrackYourDay.Tests.WorkdayComponents
{
    public class TimeAlreadyActivelyWorkdedTests
    {
        [Fact]
        public void GivenThereWasNoActivitiesOrBreaks_WhenTimeAlreadyActivelyWorkdedIsBeingCalculated_ThenTimeAlreadyActivelyWorkdedIsEqualTo0Hours()
        {
            // Arrange
            var endedActivities = new List<EndedActivity>();
            var endedBreaks = new List<EndedBreak>();

            // Act
            var workday = Workday.CreateBasedOn(endedActivities, endedBreaks);

            // Assert
            workday.TimeAlreadyActivelyWorkded.Should().Be(TimeSpan.FromHours(0));
        }

        [Fact]
        public void GivenThereWasNoActivitiesAnd50MinutesOfBreaks_WhenTimeAlreadyActivelyWorkdedIsBeingCalculated_ThenTimeAlreadyActivelyWorkdedIsEqualTo0Hours()
        {
            // Arrange
            var endedActivities = new List<EndedActivity>();
            var endedBreaks = new List<EndedBreak>
            {
                new EndedBreak(DateTime.Parse("2000-01-01 00:00"), DateTime.Parse("2000-01-01 00:50"), "Test Break")
            };

            // Act
            var workday = Workday.CreateBasedOn(endedActivities, endedBreaks);

            // Assert
            workday.TimeAlreadyActivelyWorkded.Should().Be(TimeSpan.FromHours(0));
        }

        [Fact]
        public void GivenThereWas1HourOfActivitiesAnd50MinutesOfBreaks_WhenTimeAlreadyActivelyWorkdedIsBeingCalculated_ThenTimeAlreadyActivelyWorkdedIsEqualTo6HoursAnd10Minutes()
        {
            // Arrange
            var endedActivities = new List<EndedActivity>
            {
                new EndedActivity(DateTime.Parse("2000-01-01 00:00"), DateTime.Parse("2000-01-01 00:10"), ActivityTypeFactory.FocusOnApplicationActivityType("Test application"))
            };
            var endedBreaks = new List<EndedBreak>
            {
                new EndedBreak(DateTime.Parse("2000-01-01 00:00"), DateTime.Parse("2000-01-01 00:50"), "Test Break")
            };

            // Act
            var workday = Workday.CreateBasedOn(endedActivities, endedBreaks);

            // Assert
            workday.TimeAlreadyActivelyWorkded.Should().Be(TimeSpan.FromHours(0).Add(TimeSpan.FromMinutes(10)));
        }
    }
}
