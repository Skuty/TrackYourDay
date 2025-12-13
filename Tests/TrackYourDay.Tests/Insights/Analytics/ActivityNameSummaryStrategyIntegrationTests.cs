using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TrackYourDay.Core;
using TrackYourDay.Core.ApplicationTrackers.Breaks;
using TrackYourDay.Core.Insights.Analytics;
using TrackYourDay.Core.SystemTrackers;
using TrackYourDay.Tests.TestHelpers;
using MediatR;

namespace TrackYourDay.Tests.Insights.Analytics
{
    public class ActivityNameSummaryStrategyIntegrationTests : IDisposable
    {
        private readonly Mock<ILogger<ActivityNameSummaryStrategy>> _strategyLoggerMock;
        private readonly Mock<ILogger<ActivitiesAnalyser>> _analyserLoggerMock;
        private readonly Mock<IClock> _clockMock;
        private readonly Mock<IPublisher> _publisherMock;
        private ActivityNameSummaryStrategy _strategy;
        private ActivitiesAnalyser _analyser;

        public ActivityNameSummaryStrategyIntegrationTests()
        {
            _strategyLoggerMock = new Mock<ILogger<ActivityNameSummaryStrategy>>();
            _analyserLoggerMock = new Mock<ILogger<ActivitiesAnalyser>>();
            _clockMock = new Mock<IClock>();
            _clockMock.Setup(c => c.Now).Returns(DateTime.Now);
            _publisherMock = new Mock<IPublisher>();
            
            _strategy = new ActivityNameSummaryStrategy(_strategyLoggerMock.Object);
            _analyser = new ActivitiesAnalyser(
                _clockMock.Object,
                _publisherMock.Object,
                _analyserLoggerMock.Object,
                _strategy);
        }

        public void Dispose()
        {
            _strategy?.Dispose();
            _analyser?.Dispose();
        }

        private EndedActivity CreateActivity(DateTime start, DateTime end, string description)
        {
            return new EndedActivity(start, end, new TestSystemState(description));
        }

        [Fact]
        public void GivenActivitiesWithBreaks_WhenGetGroupedActivities_ThenBreakTimeIsSubtractedFromOverlappingActivities()
        {
            // Given
            var now = DateTime.Now;
            
            // Two activities with the same name
            var activity1 = CreateActivity(now.AddHours(-3), now.AddHours(-2), "Working on Feature A");
            var activity2 = CreateActivity(now.AddHours(-1), now, "Working on Feature A");
            
            // A break that overlaps with the first activity
            var break1 = new EndedBreak(
                Guid.NewGuid(),
                now.AddHours(-2.5), // 30 minutes into the first activity
                now.AddHours(-2.25), // 15 minutes break
                "Coffee Break");
            
            _analyser.Analyse(activity1);
            _analyser.Analyse(activity2);
            _analyser.Analyse(break1);
            
            // When
            var result = _analyser.GetGroupedActivities();
            
            // Then
            result.Should().ContainSingle();
            result.First().Description.Should().Be("Working on Feature A");
            // Total activity time: 2 hours (1h + 1h)
            // Break time: 15 minutes overlapping with first activity
            // Expected duration: 2h - 15min = 1h 45min
            result.First().Duration.Should().Be(TimeSpan.FromMinutes(105));
        }

        [Fact]
        public void GivenActivitiesWithNonOverlappingBreaks_WhenGetGroupedActivities_ThenBreakDoesNotAffectDuration()
        {
            // Given
            var now = DateTime.Now;
            
            var activity1 = CreateActivity(now.AddHours(-3), now.AddHours(-2), "Working on Feature A");
            
            // A break that does not overlap with the activity
            var break1 = new EndedBreak(
                Guid.NewGuid(),
                now.AddHours(-1.5),
                now.AddHours(-1),
                "Lunch Break");
            
            _analyser.Analyse(activity1);
            _analyser.Analyse(break1);
            
            // When
            var result = _analyser.GetGroupedActivities();
            
            // Then
            result.Should().ContainSingle();
            result.First().Description.Should().Be("Working on Feature A");
            // No overlap, so full duration should be preserved
            result.First().Duration.Should().Be(TimeSpan.FromHours(1));
        }

        [Fact]
        public void GivenMultipleActivitiesSameNameWithMultipleBreaks_WhenGetGroupedActivities_ThenAllOverlappingBreaksAreSubtracted()
        {
            // Given
            var now = DateTime.Now;
            
            var activity1 = CreateActivity(now.AddHours(-4), now.AddHours(-3), "Coding");
            var activity2 = CreateActivity(now.AddHours(-2), now.AddHours(-1), "Coding");
            
            // Break overlapping with first activity
            var break1 = new EndedBreak(
                Guid.NewGuid(),
                now.AddHours(-3.5),
                now.AddHours(-3.25), // 15 min break
                "Short Break");
            
            // Break overlapping with second activity
            var break2 = new EndedBreak(
                Guid.NewGuid(),
                now.AddHours(-1.5),
                now.AddHours(-1.25), // 15 min break
                "Coffee Break");
            
            _analyser.Analyse(activity1);
            _analyser.Analyse(activity2);
            _analyser.Analyse(break1);
            _analyser.Analyse(break2);
            
            // When
            var result = _analyser.GetGroupedActivities();
            
            // Then
            result.Should().ContainSingle();
            result.First().Description.Should().Be("Coding");
            // Total: 2h - 15min - 15min = 1h 30min
            result.First().Duration.Should().Be(TimeSpan.FromMinutes(90));
        }

        [Fact]
        public void GivenDifferentActivitiesWithSharedBreak_WhenGetGroupedActivities_ThenEachActivityGroupIsReducedByOverlappingBreak()
        {
            // Given
            var now = DateTime.Now;
            
            // Different activities during the same time period
            var activity1 = CreateActivity(now.AddHours(-2), now.AddHours(-1), "Coding");
            var activity2 = CreateActivity(now.AddHours(-2), now.AddHours(-1), "Code Review");
            
            // Break overlapping with both
            var break1 = new EndedBreak(
                Guid.NewGuid(),
                now.AddHours(-1.5),
                now.AddHours(-1.25), // 15 min break
                "Break");
            
            _analyser.Analyse(activity1);
            _analyser.Analyse(activity2);
            _analyser.Analyse(break1);
            
            // When
            var result = _analyser.GetGroupedActivities();
            
            // Then
            result.Should().HaveCount(2);
            result.Should().Contain(g => g.Description == "Coding" && g.Duration == TimeSpan.FromMinutes(45));
            result.Should().Contain(g => g.Description == "Code Review" && g.Duration == TimeSpan.FromMinutes(45));
        }
    }
}
