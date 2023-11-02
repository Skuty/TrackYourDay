using FluentAssertions;
using TrackYourDay.Core;
using TrackYourDay.Core.Activities;
using TrackYourDay.Core.Breaks;

namespace TrackYourDay.Tests.WorkdayComponents
{
    public class BreaksLeftTests
    {
        [Fact]
        public void GivenThereWasNoBreaks_WhenBreaksAreBeingCalculated_ThenBreaksLeftAreEqualTo50Minutes()
        {
            // Arrange
            var endedActivities = new List<EndedActivity>();
            var endedBreaks = new List<EndedBreak>();

            // Act
            var workday = Workday.CreateBasedOn(endedActivities, endedBreaks);

            // Assert
            workday.BreakTimeLeft.Should().Be(TimeSpan.FromMinutes(50));
        }

        [Fact]
        public void GivenThereWas15MinutesOfBreaks_WhenBreaksAreBeingCalculated_ThenBreaksLeftAreEqualTo35Minutes()
        {
            // Arrange
            var endedActivities = new List<EndedActivity>();
            var endedBreaks = new List<EndedBreak>
            {
                new EndedBreak(DateTime.Parse("2000-01-01 00:00"), DateTime.Parse("2000-01-01 00:15"), "Test Break")
            };

            // Act
            var workday = Workday.CreateBasedOn(endedActivities, endedBreaks);

            // Assert
            workday.BreakTimeLeft.Should().Be(TimeSpan.FromMinutes(35));
        }

        [Fact]
        public void GivenThereWas50MinutesOfBreaks_WhenBreaksAreBeingCalculated_ThenBreaksLeftAreEqualTo0Minutes()
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
            workday.BreakTimeLeft.Should().Be(TimeSpan.FromMinutes(0));
        }

        // TODO: split in summary activities and breaks to avoid problems with properly calculating break time
        // due to lack of possibility to fully autorecognize breaks - temporarily
    }
}
