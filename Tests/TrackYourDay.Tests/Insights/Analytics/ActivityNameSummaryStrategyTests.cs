using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TrackYourDay.Core.Insights.Analytics;
using TrackYourDay.Core.SystemTrackers;
using TrackYourDay.Tests.TestHelpers;

namespace TrackYourDay.Tests.Insights.Analytics
{
    public class ActivityNameSummaryStrategyTests : IDisposable
    {
        private readonly Mock<ILogger<ActivityNameSummaryStrategy>> _loggerMock;
        private ActivityNameSummaryStrategy _sut;

        public ActivityNameSummaryStrategyTests()
        {
            _loggerMock = new Mock<ILogger<ActivityNameSummaryStrategy>>();
            _sut = new ActivityNameSummaryStrategy(_loggerMock.Object);
        }

        public void Dispose()
        {
            _sut?.Dispose();
        }

        private EndedActivity CreateActivity(DateTime start, DateTime end, string description)
        {
            return new EndedActivity(start, end, new TestSystemState(description));
        }

        [Fact]
        public void GivenNoActivities_WhenGenerateIsCalled_ThenReturnsEmptyList()
        {
            // Given
            var activities = new List<EndedActivity>();

            // When
            var result = _sut.Generate(activities);

            // Then
            result.Should().BeEmpty();
        }

        [Fact]
        public void GivenActivitiesWithSameName_WhenGenerateIsCalled_ThenGroupsTogether()
        {
            // Given
            var now = DateTime.Now;
            var activities = new List<EndedActivity>
            {
                CreateActivity(now.AddHours(-3), now.AddHours(-2), "Working on feature A"),
                CreateActivity(now.AddHours(-2), now.AddHours(-1), "Working on feature A"),
                CreateActivity(now.AddHours(-1), now, "Working on feature A")
            };

            // When
            var result = _sut.Generate(activities);

            // Then
            result.Should().ContainSingle();
            result.First().Description.Should().Be("Working on feature A");
            result.First().Duration.Should().Be(TimeSpan.FromHours(3));
        }

        [Fact]
        public void GivenActivitiesWithDifferentNames_WhenGenerateIsCalled_ThenGroupsSeparately()
        {
            // Given
            var now = DateTime.Now;
            var activities = new List<EndedActivity>
            {
                CreateActivity(now.AddHours(-3), now.AddHours(-2), "Working on feature A"),
                CreateActivity(now.AddHours(-2), now.AddHours(-1), "Working on feature B"),
                CreateActivity(now.AddHours(-1), now, "Working on feature C")
            };

            // When
            var result = _sut.Generate(activities);

            // Then
            result.Should().HaveCount(3);
            result.Should().Contain(g => g.Description == "Working on feature A" && g.Duration == TimeSpan.FromHours(1));
            result.Should().Contain(g => g.Description == "Working on feature B" && g.Duration == TimeSpan.FromHours(1));
            result.Should().Contain(g => g.Description == "Working on feature C" && g.Duration == TimeSpan.FromHours(1));
        }

        [Fact]
        public void GivenMixedActivities_WhenGenerateIsCalled_ThenGroupsByName()
        {
            // Given
            var now = DateTime.Now;
            var activities = new List<EndedActivity>
            {
                CreateActivity(now.AddHours(-5), now.AddHours(-4.5), "Code Review"),
                CreateActivity(now.AddHours(-4.5), now.AddHours(-4), "Testing"),
                CreateActivity(now.AddHours(-4), now.AddHours(-3.5), "Code Review"),
                CreateActivity(now.AddHours(-3.5), now.AddHours(-3), "Meeting"),
                CreateActivity(now.AddHours(-3), now.AddHours(-2.5), "Testing"),
                CreateActivity(now.AddHours(-2.5), now.AddHours(-2), "Code Review")
            };

            // When
            var result = _sut.Generate(activities);

            // Then
            result.Should().HaveCount(3);
            result.Should().Contain(g => g.Description == "Code Review" && g.Duration == TimeSpan.FromHours(1.5));
            result.Should().Contain(g => g.Description == "Testing" && g.Duration == TimeSpan.FromHours(1));
            result.Should().Contain(g => g.Description == "Meeting" && g.Duration == TimeSpan.FromHours(0.5));
        }

        [Fact]
        public void GivenActivitiesOnDifferentDays_WhenGenerateIsCalled_ThenGroupsByNameOnly()
        {
            // Given
            var activity1 = CreateActivity(
                new DateTime(2023, 1, 1, 10, 0, 0),
                new DateTime(2023, 1, 1, 11, 0, 0),
                "Working on feature");

            var activity2 = CreateActivity(
                new DateTime(2023, 1, 2, 10, 0, 0),
                new DateTime(2023, 1, 2, 11, 0, 0),
                "Working on feature");

            var activity3 = CreateActivity(
                new DateTime(2023, 1, 1, 14, 0, 0),
                new DateTime(2023, 1, 1, 15, 0, 0),
                "Working on feature");

            var activities = new List<EndedActivity> { activity1, activity2, activity3 };

            // When
            var result = _sut.Generate(activities);

            // Then
            result.Should().ContainSingle();
            result.First().Description.Should().Be("Working on feature");
            result.First().Duration.Should().Be(TimeSpan.FromHours(3));
        }

        [Fact]
        public void GivenActivitiesWithMultipleNamesOnMultipleDays_WhenGenerateIsCalled_ThenGroupsByNameAcrossDays()
        {
            // Given
            var activities = new List<EndedActivity>
            {
                CreateActivity(new DateTime(2023, 1, 1, 9, 0, 0), new DateTime(2023, 1, 1, 10, 0, 0), "Coding"),
                CreateActivity(new DateTime(2023, 1, 1, 10, 0, 0), new DateTime(2023, 1, 1, 11, 0, 0), "Meeting"),
                CreateActivity(new DateTime(2023, 1, 1, 14, 0, 0), new DateTime(2023, 1, 1, 15, 0, 0), "Coding"),
                CreateActivity(new DateTime(2023, 1, 2, 9, 0, 0), new DateTime(2023, 1, 2, 10, 0, 0), "Coding"),
                CreateActivity(new DateTime(2023, 1, 2, 10, 0, 0), new DateTime(2023, 1, 2, 11, 0, 0), "Meeting"),
            };

            // When
            var result = _sut.Generate(activities);

            // Then
            result.Should().HaveCount(2);
            result.Should().Contain(g => g.Description == "Coding" && g.Duration == TimeSpan.FromHours(3));
            result.Should().Contain(g => g.Description == "Meeting" && g.Duration == TimeSpan.FromHours(2));
        }

        [Fact]
        public void GivenNullActivities_WhenGenerateIsCalled_ThenThrowsArgumentNullException()
        {
            // Given
            IEnumerable<EndedActivity> activities = null;

            // When
            Action act = () => _sut.Generate(activities);

            // Then
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void GivenStrategyName_WhenAccessed_ThenReturnsCorrectName()
        {
            // When
            var strategyName = _sut.StrategyName;

            // Then
            strategyName.Should().Be("Activity Name Groups");
        }

        [Fact]
        public void GivenActivitiesWithVeryShortDurations_WhenGenerateIsCalled_ThenGroupsCorrectly()
        {
            // Given
            var now = DateTime.Now;
            var activities = new List<EndedActivity>
            {
                CreateActivity(now.AddMinutes(-30), now.AddMinutes(-25), "Quick task"),
                CreateActivity(now.AddMinutes(-20), now.AddMinutes(-15), "Quick task"),
                CreateActivity(now.AddMinutes(-10), now.AddMinutes(-5), "Quick task")
            };

            // When
            var result = _sut.Generate(activities);

            // Then
            result.Should().ContainSingle();
            result.First().Description.Should().Be("Quick task");
            result.First().Duration.Should().Be(TimeSpan.FromMinutes(15));
        }

        [Fact]
        public void GivenActivitiesWithSpecialCharacters_WhenGenerateIsCalled_ThenGroupsByExactName()
        {
            // Given
            var now = DateTime.Now;
            var activities = new List<EndedActivity>
            {
                CreateActivity(now.AddHours(-3), now.AddHours(-2), "PROJ-123: Feature Implementation"),
                CreateActivity(now.AddHours(-2), now.AddHours(-1), "PROJ-123: Feature Implementation"),
                CreateActivity(now.AddHours(-1), now, "PROJ-124: Bug Fix")
            };

            // When
            var result = _sut.Generate(activities);

            // Then
            result.Should().HaveCount(2);
            result.Should().Contain(g => g.Description == "PROJ-123: Feature Implementation" && g.Duration == TimeSpan.FromHours(2));
            result.Should().Contain(g => g.Description == "PROJ-124: Bug Fix" && g.Duration == TimeSpan.FromHours(1));
        }

        [Fact]
        public void GivenSingleActivity_WhenGenerateIsCalled_ThenReturnsOneGroup()
        {
            // Given
            var now = DateTime.Now;
            var activities = new List<EndedActivity>
            {
                CreateActivity(now.AddHours(-1), now, "Single Activity")
            };

            // When
            var result = _sut.Generate(activities);

            // Then
            result.Should().ContainSingle();
            result.First().Description.Should().Be("Single Activity");
            result.First().Duration.Should().Be(TimeSpan.FromHours(1));
        }
    }
}
