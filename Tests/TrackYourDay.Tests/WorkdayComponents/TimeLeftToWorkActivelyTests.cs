using FluentAssertions;
using TrackYourDay.Core;
using TrackYourDay.Core.Activities;
using TrackYourDay.Core.Activities.SystemStates;
using TrackYourDay.Core.Breaks;

namespace TrackYourDay.Tests.WorkdayComponents
{
    public class TimeLeftToWorkActivelyTests
    {
        [Fact]
        public void GivenThereWasNoActivitiesOrBreaks_WhenTimeLeftToWorkActivelyIsBeingCalculated_ThenTimeLeftToWorkIsEqualTo7HoursAnd10Minutes()
        {
            // Arrange
            var endedActivities = new List<EndedActivity>();
            var endedBreaks = new List<EndedBreak>();

            // Act
            var workday = Workday.CreateBasedOn(endedActivities, endedBreaks);

            // Assert
            workday.TimeLeftToWorkActively.Should().Be(TimeSpan.FromHours(7).Add(TimeSpan.FromMinutes(10)));
        }

        [Fact]
        public void GivenThereWasNoActivitiesAndThereWas50MinutesOfBreaks_WhenTimeLeftToWorkActivelyIsBeingCalculated_ThenTimeLeftToWorkIsEqualTo7HoursAnd10Minutes()
        {
            // Arrange
            var endedActivities = new List<EndedActivity>();
            var endedBreaks = new List<EndedBreak>
            {
                new EndedBreak(Guid.Empty, DateTime.Parse("2000-01-01 00:00"), DateTime.Parse("2000-01-01 00:50"), "Test Break")
            };

            // Act
            var workday = Workday.CreateBasedOn(endedActivities, endedBreaks);

            // Assert
            workday.TimeLeftToWorkActively.Should().Be(TimeSpan.FromHours(7).Add(TimeSpan.FromMinutes(10)));
        }

        [Fact]
        public void GivenThereWasNoActivitiesAndThereWas60MinutesOfBreaks_WhenTimeLeftToWorkActivelyIsBeingCalculated_ThenTimeLeftToWorkIsEqualTo7HoursAnd10Minutes()
        {
            // Arrange
            var endedActivities = new List<EndedActivity>();
            var endedBreaks = new List<EndedBreak>
            {
                new EndedBreak(Guid.Empty, DateTime.Parse("2000-01-01 00:00"), DateTime.Parse("2000-01-01 01:00"), "Test Break")
            };

            // Act
            var workday = Workday.CreateBasedOn(endedActivities, endedBreaks);

            // Assert
            workday.TimeLeftToWorkActively.Should().Be(TimeSpan.FromHours(7).Add(TimeSpan.FromMinutes(10)));
        }

        [Fact]
        public void GivenThereWas7HoursOfActivitiesAndNoBreaks_WhenTimeLeftToWorkActivelyIsBeingCalculated_ThenTimeLeftToWorkActivelyIsEqualTo10Minutes()
        {
            // Arrange
            var endedActivities = new List<EndedActivity>
            {
                new EndedActivity(DateTime.Parse("2000-01-01 00:00"), DateTime.Parse("2000-01-01 07:00"), SystemStateFactory.FocusOnApplicationState("Test application"))
            };
            var endedBreaks = new List<EndedBreak>();

            // Act
            var workday = Workday.CreateBasedOn(endedActivities, endedBreaks);

            // Assert
            workday.TimeLeftToWorkActively.Should().Be(TimeSpan.FromMinutes(10));
        }

        [Fact]
        public void GivenThereWas6HoursOfAllActivitiesAndThereWas30MinutesOfBreaksWithinIt_WhenTimeLeftToWorkActivelyIsBeingCalculated_ThenTimeLeftToWorkIsEqualTo1HourAnd40Minutes()
        {
            // Arrange
            var endedActivities = new List<EndedActivity>
            {
                new EndedActivity(DateTime.Parse("2000-01-01 00:00"), DateTime.Parse("2000-01-01 06:00"), SystemStateFactory.FocusOnApplicationState("Test application"))
            };
            var endedBreaks = new List<EndedBreak>
            {
                new EndedBreak(Guid.Empty, DateTime.Parse("2000-01-01 00:00"), DateTime.Parse("2000-01-01 00:30"), "Test Break")
            };

            // Act
            var workday = Workday.CreateBasedOn(endedActivities, endedBreaks);

            // Assert
            workday.TimeLeftToWorkActively.Should().Be(TimeSpan.FromHours(1).Add(TimeSpan.FromMinutes(40)));
        }

        [Fact]
        public void GivenThereWas7HoursAnd10MinutesMinutesOfActivitiesAndNoBreaks_WhenTimeLeftToWorkActivelyIsBeingCalculated_ThenTimeLeftToWorkIsEqualTo0Minutes()
        {
            // Arrange
            var endedActivities = new List<EndedActivity>
            {
                new EndedActivity(DateTime.Parse("2000-01-01 00:00"), DateTime.Parse("2000-01-01 07:10"), SystemStateFactory.FocusOnApplicationState("Test application"))
            };
            var endedBreaks = new List<EndedBreak>();

            // Act
            var workday = Workday.CreateBasedOn(endedActivities, endedBreaks);

            // Assert
            workday.TimeLeftToWorkActively.Should().Be(TimeSpan.FromMinutes(0));
        }

        [Fact]
        public void GivenThereWas8HoursOfAllActivitiesAndThereWas50MinutesOfBreaksWithinIt_WhenTimeLeftToWorkActivelyIsBeingCalculated_ThenTimeLeftToWorkIsEqualTo0Minutes()
        {
            // Arrange
            var endedActivities = new List<EndedActivity>
            {
                new EndedActivity(DateTime.Parse("2000-01-01 00:00"), DateTime.Parse("2000-01-01 08:00"), SystemStateFactory.FocusOnApplicationState("Test application"))
            };
            var endedBreaks = new List<EndedBreak>
            {
                new EndedBreak(Guid.Empty, DateTime.Parse("2000-01-01 00:00"), DateTime.Parse("2000-01-01 00:50"), "Test Break")
            };

            // Act
            var workday = Workday.CreateBasedOn(endedActivities, endedBreaks);

            // Assert
            workday.TimeLeftToWorkActively.Should().Be(TimeSpan.FromHours(0));
        }

        [Fact]
        public void GivenThereWas8HoursOfAllActivitiesAndThereWas60MinutesOfBreaksWithinIt_WhenTimeLeftToWorkActivelyIsBeingCalculated_ThenTimeLeftToWorkIsEqualTo10Minutes()
        {
            // Arrange
            var endedActivities = new List<EndedActivity>
            {
                new EndedActivity(DateTime.Parse("2000-01-01 00:00"), DateTime.Parse("2000-01-01 08:00"), SystemStateFactory.FocusOnApplicationState("Test application"))
            };
            var endedBreaks = new List<EndedBreak>
            {
                new EndedBreak(Guid.Empty, DateTime.Parse("2000-01-01 00:00"), DateTime.Parse("2000-01-01 01:00"), "Test Break")
            };

            // Act
            var workday = Workday.CreateBasedOn(endedActivities, endedBreaks);

            // Assert
            workday.TimeLeftToWorkActively.Should().Be(TimeSpan.FromMinutes(10));
        }

        [Fact]
        public void GivenThereWas3HoursAnd30MinutesMinutesOfActivitiesAndNoBreaks_WhenTimeLeftToWorkActivelyIsBeingCalculated_ThenTimeLeftToWorkIsEqualTo3HoursAnd40Minutes()
        {
            // Arrange
            var endedActivities = new List<EndedActivity>
            {
                new EndedActivity(DateTime.Parse("2000-01-01 00:00"), DateTime.Parse("2000-01-01 03:30"), SystemStateFactory.FocusOnApplicationState("Test application"))
            };
            var endedBreaks = new List<EndedBreak>();

            // Act
            var workday = Workday.CreateBasedOn(endedActivities, endedBreaks);

            // Assert
            workday.TimeLeftToWorkActively.Should().Be(TimeSpan.FromHours(3).Add(TimeSpan.FromMinutes(40)));
        }
    }
}
