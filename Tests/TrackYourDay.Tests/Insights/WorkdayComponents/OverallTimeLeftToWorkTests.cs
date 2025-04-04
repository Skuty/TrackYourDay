﻿using FluentAssertions;
using TrackYourDay.Core.ApplicationTrackers.Breaks;
using TrackYourDay.Core.Insights.Workdays;
using TrackYourDay.Core.SystemTrackers;
using TrackYourDay.Core.SystemTrackers.SystemStates;

namespace TrackYourDay.Tests.Insights.WorkdayComponents
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
            var workday = Workday.CreateBasedOn(TestSettingsSet.WorkdayDefinition, endedActivities, endedBreaks);

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
                new EndedBreak(Guid.Empty, DateTime.Parse("2000-01-01 00:00"), DateTime.Parse("2000-01-01 00:50"), "Test Break")
            };

            // Act
            var workday = Workday.CreateBasedOn(TestSettingsSet.WorkdayDefinition, endedActivities, endedBreaks);

            // Assert
            workday.OverallTimeLeftToWork.Should().Be(TimeSpan.FromHours(7).Add(TimeSpan.FromMinutes(10)));
        }

        [Fact]
        public void GivenThereWas1HourOfAllActivitiesAnd50MinutesOfBreaksWithinIt_WhenOverallTimeLeftToWorkIsBeingCalculated_ThenOverallTimeLeftToWorkIsEqualTo7Hours()
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
            workday.OverallTimeLeftToWork.Should().Be(TimeSpan.FromHours(7));
        }

        [Fact]
        public void GivenThereWas8HourOfAllActivitiesAndNoBreaksWithinIt_WhenOverallTimeLeftToWorkIsBeingCalculated_ThenOverallTimeLeftToWorkIsEqualTo0Hours()
        {
            // Arrange
            var endedActivities = new List<EndedActivity>
            {
                new EndedActivity(DateTime.Parse("2000-01-01 00:00"), DateTime.Parse("2000-01-01 08:00"), SystemStateFactory.FocusOnApplicationState("Test application"))
            };
            var endedBreaks = new List<EndedBreak>();

            // Act
            var workday = Workday.CreateBasedOn(TestSettingsSet.WorkdayDefinition, endedActivities, endedBreaks);

            // Assert
            workday.OverallTimeLeftToWork.Should().Be(TimeSpan.FromHours(0));
        }

        [Fact]
        public void GivenThereWas9HourOfAllActivitiesAndNoBreaksWithinIt_WhenOverallTimeLeftToWorkIsBeingCalculated_ThenOverallTimeLeftToWorkIsEqualTo0Hours()
        {
            // Arrange
            var endedActivities = new List<EndedActivity>
            {
                new EndedActivity(DateTime.Parse("2000-01-01 00:00"), DateTime.Parse("2000-01-01 09:00"), SystemStateFactory.FocusOnApplicationState("Test application"))
            };
            var endedBreaks = new List<EndedBreak>();

            // Act
            var workday = Workday.CreateBasedOn(TestSettingsSet.WorkdayDefinition, endedActivities, endedBreaks);

            // Assert
            workday.OverallTimeLeftToWork.Should().Be(TimeSpan.FromHours(0));
        }


        [Fact]
        public void GivenThereWas8HourOfAllActivitiesAnd10MinutesOfBreaksWithinIt_WhenOverallTimeLeftToWorkIsBeingCalculated_ThenOverallTimeLeftToWorkIsEqualTo0Hours()
        {
            // Arrange
            var endedActivities = new List<EndedActivity>
            {
                new EndedActivity(DateTime.Parse("2000-01-01 00:00"), DateTime.Parse("2000-01-01 08:00"), SystemStateFactory.FocusOnApplicationState("Test application"))
            };
            var endedBreaks = new List<EndedBreak>()
            {
                new EndedBreak(Guid.Empty, DateTime.Parse("2000-01-01 00:00"), DateTime.Parse("2000-01-01 00:10"), "Test Break")
            };

            // Act
            var workday = Workday.CreateBasedOn(TestSettingsSet.WorkdayDefinition, endedActivities, endedBreaks);

            // Assert
            workday.OverallTimeLeftToWork.Should().Be(TimeSpan.FromHours(0));
        }

    }
}
