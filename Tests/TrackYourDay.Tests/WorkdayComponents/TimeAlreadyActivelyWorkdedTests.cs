﻿using FluentAssertions;
using TrackYourDay.Core.Activities;
using TrackYourDay.Core.Activities.SystemStates;
using TrackYourDay.Core.Breaks;
using TrackYourDay.Core.Workdays;

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
            var workday = Workday.CreateBasedOn(TestSettingsSet.WorkdayDefinition, endedActivities, endedBreaks);

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
                new EndedBreak(Guid.Empty, DateTime.Parse("2000-01-01 00:00"), DateTime.Parse("2000-01-01 00:50"), "Test Break")
            };

            // Act
            var workday = Workday.CreateBasedOn(TestSettingsSet.WorkdayDefinition, endedActivities, endedBreaks);

            // Assert
            workday.TimeAlreadyActivelyWorkded.Should().Be(TimeSpan.FromHours(0));
        }

        [Fact]
        public void GivenThereWas1HourOfActivitiesAnd50MinutesOfBreaks_WhenTimeAlreadyActivelyWorkdedIsBeingCalculated_ThenTimeAlreadyActivelyWorkdedIsEqualTo10Minutes()
        {
            // Arrange
            var endedActivities = new List<EndedActivity>
            {
                new EndedActivity(DateTime.Parse("2000-01-01 00:00"), DateTime.Parse("2000-01-01 01:00"), SystemStateFactory.FocusOnApplicationState("Test application"))
            };
            var endedBreaks = new List<EndedBreak>
            {
                new EndedBreak(Guid.Empty, DateTime.Parse("2000-01-01 00:00"), DateTime.Parse("2000-01-01 00:50"), "Test Break")
            };

            // Act
            var workday = Workday.CreateBasedOn(TestSettingsSet.WorkdayDefinition, endedActivities, endedBreaks);

            // Assert
            workday.TimeAlreadyActivelyWorkded.Should().Be(TimeSpan.FromMinutes(10));
        }
    }
}
