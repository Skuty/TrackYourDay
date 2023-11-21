using FluentAssertions;
using TrackYourDay.Core;
using TrackYourDay.Core.Activities;
using TrackYourDay.Core.Activities.SystemStates;
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
            workday.OverhoursTime.Should().Be(TimeSpan.FromHours(0));
        }

        [Fact]
        public void GivenThereWas8HoursOfAllActivitiesAnd50MinutesOfBreaksWithinIt_WhenOverhoursAreBeingCalculated_ThenOverhoursEqualsTo0Minutes()
        {
            // Arrange
            var endedActivities = new List<EndedActivity>
            {
                new EndedActivity(DateTime.Parse("2000-01-01 00:00"), DateTime.Parse("2000-01-01 08:00"), SystemStateFactory.FocusOnApplicationState("Test application"))
            };
            var endedBreaks = new List<EndedBreak>
            {
                new EndedBreak(DateTime.Parse("2000-01-01 00:00"), DateTime.Parse("2000-01-01 00:50"), "Test Break")
            };

            // Act
            var workday = Workday.CreateBasedOn(endedActivities, endedBreaks);

            // Assert
            workday.OverhoursTime.Should().Be(TimeSpan.FromMinutes(0));
        }

        [Fact]
        public void GivenThereWas7HoursAnd10MinutesOfActivitiesAnd80MinutesOfBreaks_WhenOverhoursAreBeingCalculated_ThenOverhoursEqualsTo0Hours()
        {
            // Arrange
            var endedActivities = new List<EndedActivity>
            {
                new EndedActivity(DateTime.Parse("2000-01-01 00:00"), DateTime.Parse("2000-01-01 07:10"), SystemStateFactory.FocusOnApplicationState("Test application"))
            };
            var endedBreaks = new List<EndedBreak>
            {
                new EndedBreak(DateTime.Parse("2000-01-01 00:00"), DateTime.Parse("2000-01-01 01:20"), "Test Break")
            };

            // Act
            var workday = Workday.CreateBasedOn(endedActivities, endedBreaks);

            // Assert
            workday.OverhoursTime.Should().Be(TimeSpan.FromHours(0));
        }

        [Fact]
        public void GivenThereWas8HoursOfAllActivitiesAndNoBreaks_WhenOverhoursAreBeingCalculated_ThenOverhoursEqualsTo50Minutes()
        {
            // Arrange
            var endedActivities = new List<EndedActivity>
            {
                new EndedActivity(DateTime.Parse("2000-01-01 00:00"), DateTime.Parse("2000-01-01 08:00"), SystemStateFactory.FocusOnApplicationState("Test application"))
            };
            var endedBreaks = new List<EndedBreak>();
            
            // Act
            var workday = Workday.CreateBasedOn(endedActivities, endedBreaks);

            // Assert
            workday.OverhoursTime.Should().Be(TimeSpan.FromMinutes(50));
        }

        [Fact]
        public void GivenThereWas8HoursAnd40MinutesOfAllActivitiesAnd50MinutesOfBreaksWithinIt_WhenOverhoursAreBeingCalculated_ThenOverhoursEqualsTo40Minutes()
        {
            // Arrange
            var endedActivities = new List<EndedActivity>
            {
                new EndedActivity(DateTime.Parse("2000-01-01 00:00"), DateTime.Parse("2000-01-01 08:40"), SystemStateFactory.FocusOnApplicationState("Test application"))
            };
            var endedBreaks = new List<EndedBreak>
            {
                new EndedBreak(DateTime.Parse("2000-01-01 00:00"), DateTime.Parse("2000-01-01 00:50"), "Test Break")
            };

            // Act
            var workday = Workday.CreateBasedOn(endedActivities, endedBreaks);

            // Assert
            workday.OverhoursTime.Should().Be(TimeSpan.FromMinutes(40)); 
        }
    }
}
