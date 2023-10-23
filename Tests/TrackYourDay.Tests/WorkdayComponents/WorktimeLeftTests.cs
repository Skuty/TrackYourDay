using FluentAssertions;
using TrackYourDay.Core;
using TrackYourDay.Core.Activities;
using TrackYourDay.Core.Breaks;

namespace TrackYourDay.Tests.WorkdayComponents
{
    public class WorktimeLeftTests
    {
        [Fact]
        public void GivenThereWasNoActivitiesOrBreaks_WhenWorktimeLeftIsBeingCalculated_ThenWorkTimeLeftIsEqualTo8Hours()
        {
            // Arrange
            var endedActivities = new List<EndedActivity>();
            var endedBreaks = new List<EndedBreak>();

            // Act
            var workday = Workday.CreateBasedOn(endedActivities, endedBreaks);

            // Assert
            workday.WorktimeLeft.Should().Be(TimeSpan.FromHours(8));
        }

        [Fact]
        public void GivenThereWasNoActivitiesAndThereWas50MinutesOfBreaks_WhenWorktimeLeftIsBeingCalculated_ThenWorkTimeLeftIsEqualTo7HoursAnd10Minutes()
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
            workday.WorktimeLeft.Should().Be(TimeSpan.FromHours(7).Add(TimeSpan.FromMinutes(10)));
        }

        [Fact]
        public void GivenThereWas7HoursAnd10MinutesMinutesOfActivitiesAndThereWas50MinutesOfBreaks_WhenWorktimeLeftIsBeingCalculated_ThenWorkTimeLeftIsEqualTo0Minutes()
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
            workday.WorktimeLeft.Should().Be(TimeSpan.FromMinutes(0));
        }

        [Fact]
        public void GivenThereWas7HoursAnd10MinutesMinutesOfActivitiesAndNoBreaks_WhenWorktimeLeftIsBeingCalculated_ThenWorkTimeLeftIsEqualTo50Minutes()
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
            workday.WorktimeLeft.Should().Be(TimeSpan.FromMinutes(50));
        }

        [Fact]
        public void GivenThereWas3HoursAnd30MinutesMinutesOfActivitiesAndNoBreaks_WhenWorktimeLeftIsBeingCalculated_ThenWorkTimeLeftIsEqualTo4HoursAnd30Minutes()
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
            workday.WorktimeLeft.Should().Be(TimeSpan.FromHours(4).Add(TimeSpan.FromMinutes(30)));
        }
    }
}
