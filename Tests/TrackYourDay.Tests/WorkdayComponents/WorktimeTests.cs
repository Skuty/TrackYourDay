using FluentAssertions;
using TrackYourDay.Core;
using TrackYourDay.Core.Activities;
using TrackYourDay.Core.Breaks;

namespace TrackYourDay.Tests.WorkdayComponents
{
    public class WorktimeTests
    {
        [Fact]
        public void GivenThereWasNoActivitiesOrBreaks_WhenTimeLeftToWorkIsBeingCalculated_ThenTimeLeftToWorkIsEqualTo7HoursAnd10Minutes()
        {
            // Arrange
            var endedActivities = new List<EndedActivity>();
            var endedBreaks = new List<EndedBreak>();

            // Act
            var workday = Workday.CreateBasedOn(endedActivities, endedBreaks);

            // Assert
            workday.TimeLeftToWork.Should().Be(TimeSpan.FromHours(7).Add(TimeSpan.FromMinutes(10)));
        }

        [Fact]
        public void GivenThereWasNoActivitiesAndThereWas50MinutesOfBreaks_WhenTimeLeftToWorkIsBeingCalculated_ThenTimeLeftToWorkIsEqualTo7HoursAnd10Minutes()
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
            workday.TimeLeftToWork.Should().Be(TimeSpan.FromHours(7).Add(TimeSpan.FromMinutes(10)));
        }

        [Fact]
        public void GivenThereWasNoActivitiesAndThereWas60MinutesOfBreaks_WhenTimeLeftToWorkIsBeingCalculated_ThenTimeLeftToWorkIsEqualTo7HoursAnd10Minutes()
        {
            // Arrange
            var endedActivities = new List<EndedActivity>();
            var endedBreaks = new List<EndedBreak>
            {
                new EndedBreak(DateTime.Parse("2000-01-01 00:00"), DateTime.Parse("2000-01-01 01:00"), "Test Break")
            };

            // Act
            var workday = Workday.CreateBasedOn(endedActivities, endedBreaks);

            // Assert
            workday.TimeLeftToWork.Should().Be(TimeSpan.FromHours(7).Add(TimeSpan.FromMinutes(10)));
        }


        [Fact]
        public void GivenThereWas7HoursAnd10MinutesMinutesOfActivitiesAndThereWas50MinutesOfBreaks_WhenTimeLeftToWorkIsBeingCalculated_ThenTimeLeftToWorkIsEqualTo0Minutes()
        {
            // Arrange
            var endedActivities = new List<EndedActivity>
            {
                new EndedActivity(DateTime.Parse("2000-01-01 00:00"), DateTime.Parse("2000-01-01 07:10"), ActivityTypeFactory.FocusOnApplicationActivityType("Test application"))
            };
            var endedBreaks = new List<EndedBreak>
            {
                new EndedBreak(DateTime.Parse("2000-01-01 00:00"), DateTime.Parse("2000-01-01 00:50"), "Test Break")
            };

            // Act
            var workday = Workday.CreateBasedOn(endedActivities, endedBreaks);

            // Assert
            workday.TimeLeftToWork.Should().Be(TimeSpan.FromMinutes(0));
        }

        [Fact]
        public void GivenThereWas7HoursAnd20MinutesMinutesOfActivitiesAndThereWas50MinutesOfBreaks_WhenTimeLeftToWorkIsBeingCalculated_ThenTimeLeftToWorkIsEqualTo0Minutes()
        {
            // Arrange
            var endedActivities = new List<EndedActivity>
            {
                new EndedActivity(DateTime.Parse("2000-01-01 00:00"), DateTime.Parse("2000-01-01 07:20"), ActivityTypeFactory.FocusOnApplicationActivityType("Test application"))
            };
            var endedBreaks = new List<EndedBreak>
            {
                new EndedBreak(DateTime.Parse("2000-01-01 00:00"), DateTime.Parse("2000-01-01 00:50"), "Test Break")
            };

            // Act
            var workday = Workday.CreateBasedOn(endedActivities, endedBreaks);

            // Assert
            workday.TimeLeftToWork.Should().Be(TimeSpan.FromMinutes(0));
        }


        [Fact]
        public void GivenThereWas7HoursAnd10MinutesMinutesOfActivitiesAndNoBreaks_WhenTimeLeftToWorkIsBeingCalculated_ThenTimeLeftToWorkIsEqualTo0Minutes()
        {
            // Arrange
            var endedActivities = new List<EndedActivity>
            {
                new EndedActivity(DateTime.Parse("2000-01-01 00:00"), DateTime.Parse("2000-01-01 07:10"), ActivityTypeFactory.FocusOnApplicationActivityType("Test application"))
            };
            var endedBreaks = new List<EndedBreak>();

            // Act
            var workday = Workday.CreateBasedOn(endedActivities, endedBreaks);

            // Assert
            workday.TimeLeftToWork.Should().Be(TimeSpan.FromMinutes(0));
        }

        [Fact]
        public void GivenThereWas3HoursAnd30MinutesMinutesOfActivitiesAndNoBreaks_WhenTimeLeftToWorkIsBeingCalculated_ThenTimeLeftToWorkIsEqualTo3HoursAnd40Minutes()
        {
            // Arrange
            var endedActivities = new List<EndedActivity>
            {
                new EndedActivity(DateTime.Parse("2000-01-01 00:00"), DateTime.Parse("2000-01-01 03:30"), ActivityTypeFactory.FocusOnApplicationActivityType("Test application"))
            };
            var endedBreaks = new List<EndedBreak>();

            // Act
            var workday = Workday.CreateBasedOn(endedActivities, endedBreaks);

            // Assert
            workday.TimeLeftToWork.Should().Be(TimeSpan.FromHours(3).Add(TimeSpan.FromMinutes(40)));
        }
    }
}
