using FluentAssertions;
using TrackYourDay.Core.ApplicationTrackers.Breaks;
using TrackYourDay.Core.Insights.Workdays;
using TrackYourDay.Core.SystemTrackers;

namespace TrackYourDay.Tests.Insights.WorkdayComponents
{
    public class BreakTests
    {
        [Fact]
        public void GivenThereWasNoBreaks_WhenBreakTimeLeftIsBeingCalculated_ThenBreakTimeLeftIsEqualTo50Minutes()
        {
            // Arrange
            var endedActivities = new List<EndedActivity>();
            var endedBreaks = new List<EndedBreak>();

            // Act
            var workday = Workday.CreateBasedOn(TestSettingsSet.WorkdayDefinition, endedActivities, endedBreaks);

            // Assert
            workday.BreakTimeLeft.Should().Be(TimeSpan.FromMinutes(50));
            workday.ValidBreakTimeUsed.Should().Be(TimeSpan.FromMinutes(0));
        }

        [Fact]
        public void GivenThereWas15MinutesOfBreaks_WhenBreakTimeLeftIsBeingCalculated_ThenBreakTimeLeftIsEqualTo35Minutes()
        {
            // Arrange
            var endedActivities = new List<EndedActivity>();
            var endedBreaks = new List<EndedBreak>
            {
                new EndedBreak(Guid.Empty, DateTime.Parse("2000-01-01 00:00"), DateTime.Parse("2000-01-01 00:15"), "Test Break")
            };

            // Act
            var workday = Workday.CreateBasedOn(TestSettingsSet.WorkdayDefinition, endedActivities, endedBreaks);

            // Assert
            workday.BreakTimeLeft.Should().Be(TimeSpan.FromMinutes(35));
            workday.ValidBreakTimeUsed.Should().Be(TimeSpan.FromMinutes(15));
        }

        [Fact]
        public void GivenThereWas50MinutesOfBreaks_WhenBreakTimeLeftIsBeingCalculated_ThenBreakTimeLeftIsEqualTo0Minutes()
        {
            // Arrange
            var endedActivities = new List<EndedActivity>();
            var endedBreaks = new List<EndedBreak>
            {
                new EndedBreak(Guid.Empty, DateTime.Parse("2000-01-01 00:00"), DateTime.Parse("2000-01-01 00:50"), "Test Break")
            };

            // Act
            var workday = Workday.CreateBasedOn(TestSettingsSet.WorkdayDefinition, endedActivities, endedBreaks);

            // Assert
            workday.BreakTimeLeft.Should().Be(TimeSpan.FromMinutes(0));
            workday.ValidBreakTimeUsed.Should().Be(TimeSpan.FromMinutes(50));
        }

        // TODO: split in summary activities and breaks to avoid problems with properly calculating break time
        // due to lack of possibility to fully autorecognize breaks - temporarily
    }
}
