using FluentAssertions;
using TrackYourDay.Core;
using TrackYourDay.Core.Activities;
using TrackYourDay.Core.Breaks;

namespace TrackYourDay.Tests.WorkdayComponents
{
    public class OverhoursTests
    {
        [Fact]
        public void GivenThereWasNoActivitiesOrBreaks_WhenOverhoursAreBeingCalculated_ThenOverhoursEqualsTo0Hours()
        {
            // Arrange
            var endedActivities = new List<EndedActivity>();
            var endedBreaks = new List<EndedBreak>();

            // Act
            var workday = Workday.CreateBasedOn(endedActivities, endedBreaks);

            // Assert
            workday.Overhours.Should().Be(TimeSpan.FromHours(0));
        }

        [Fact]
        public void GivenThereWas7HoursAnd10MinutesOfActivitiesAnd50MinutesOfBreaks_WhenOverhoursAreBeingCalculated_ThenOverhoursEqualsTo0Hours()
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
            workday.Overhours.Should().Be(TimeSpan.FromHours(0));
        }

        [Fact]
        public void GivenThereWas8HoursOfActivitiesAnd50MinutesOfBreaks_WhenOverhoursAreBeingCalculated_ThenOverhoursEqualsTo50Minutes()
        {
            // Arrange
            var endedActivities = new List<EndedActivity>
            {
                new EndedActivity(DateTime.Parse("2000-01-01 00:00"), DateTime.Parse("2000-01-01 08:00"), ActivityTypeFactory.FocusOnApplicationActivityType("Test application"))
            };
            var endedBreaks = new List<EndedBreak>
            {
                new EndedBreak(DateTime.Parse("2000-01-01 00:00"), DateTime.Parse("2000-01-01 00:50"), "Test Break")
            };

            // Act
            var workday = Workday.CreateBasedOn(endedActivities, endedBreaks);

            // Assert
            workday.Overhours.Should().Be(TimeSpan.FromMinutes(50));
        }

        [Fact]
        public void GivenThereWas9HoursAnd40MinutesOfActivitiesAnd50MinutesOfBreaks_WhenOverhoursAreBeingCalculated_ThenOverhoursEqualsTo2HoursAnd30Minutes()
        {
            // Arrange
            var endedActivities = new List<EndedActivity>
            {
                new EndedActivity(DateTime.Parse("2000-01-01 00:00"), DateTime.Parse("2000-01-01 09:40"), ActivityTypeFactory.FocusOnApplicationActivityType("Test application"))
            };
            var endedBreaks = new List<EndedBreak>
            {
                new EndedBreak(DateTime.Parse("2000-01-01 00:00"), DateTime.Parse("2000-01-01 00:50"), "Test Break")
            };

            // Act
            var workday = Workday.CreateBasedOn(endedActivities, endedBreaks);

            // Assert
            workday.Overhours.Should().Be(TimeSpan.FromHours(2).Add(TimeSpan.FromMinutes(30))); 
        }
    }
}
