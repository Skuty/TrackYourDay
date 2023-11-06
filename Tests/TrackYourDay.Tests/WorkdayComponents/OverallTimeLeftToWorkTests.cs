using FluentAssertions;
using TrackYourDay.Core;
using TrackYourDay.Core.Activities;
using TrackYourDay.Core.Breaks;

namespace TrackYourDay.Tests.WorkdayComponents
{
    public class OverallTimeLeftToWorkTests
    {
        [Fact]
        public void GivenThereWasNoActivitiesOrBreaks_WhenOverallTimeLeftToWorkIsBeingCalculated_ThenOverallTimeLeftToWorkIsEqualTo8Hours()
        {
            // Arrange
            var endedActivities = new List<EndedActivity>();
            var endedBreaks = new List<EndedBreak>();

            // Act
            var workday = Workday.CreateBasedOn(endedActivities, endedBreaks);

            // Assert
            workday.OverallTimeLeftToWork.Should().Be(TimeSpan.FromHours(8));
        }

        [Fact]
        public void GivenThereWasNoActivitiesAnd50MinutesOfBreaks_WhenOverallTimeLeftToWorkIsBeingCalculated_ThenOverallTimeLeftToWorkIsEqualTo7HoursAnd10Minutes()
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
            workday.OverallTimeLeftToWork.Should().Be(TimeSpan.FromHours(7).Add(TimeSpan.FromMinutes(10)));
        }

        [Fact]
        public void GivenThereWas1HourOfActivitiesAnd50MinutesOfBreaks_WhenOverallTimeLeftToWorkIsBeingCalculated_ThenOverallTimeLeftToWorkIsEqualTo6HoursAnd10Minutes()
        {
            // Arrange
            var endedActivities = new List<EndedActivity>
            {
                new EndedActivity(DateTime.Parse("2000-01-01 00:00"), DateTime.Parse("2000-01-01 01:00"), ActivityTypeFactory.FocusOnApplicationActivityType("Test application"))
            };
            var endedBreaks = new List<EndedBreak>
            {
                new EndedBreak(DateTime.Parse("2000-01-01 00:00"), DateTime.Parse("2000-01-01 00:50"), "Test Break")
            };

            // Act
            var workday = Workday.CreateBasedOn(endedActivities, endedBreaks);

            // Assert
            workday.OverallTimeLeftToWork.Should().Be(TimeSpan.FromHours(6).Add(TimeSpan.FromMinutes(10)));
        }
    }
}
