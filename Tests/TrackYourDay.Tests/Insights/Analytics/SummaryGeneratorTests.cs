using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TrackYourDay.Core;
using TrackYourDay.Core.Insights.Analytics;
using TrackYourDay.Core.SystemTrackers;
using TrackYourDay.Core.SystemTrackers.SystemStates;
using TrackYourDay.Tests.TestHelpers;

namespace TrackYourDay.Tests.Insights.Analytics
{
    public class SummaryGeneratorTests : IDisposable
    {
        private readonly Mock<ILogger<SummaryGenerator>> _loggerMock;
        private readonly Mock<IClock> _clockMock;
        private SummaryGenerator _sut;

        public SummaryGeneratorTests()
        {
            _loggerMock = new Mock<ILogger<SummaryGenerator>>();
            _clockMock = new Mock<IClock>();
            _sut = new SummaryGenerator(_clockMock.Object, _loggerMock.Object);
        }

        public void Dispose()
        {
            _sut.Dispose();
        }

        [Fact]
        public void GivenNoActivities_WhenGenerateIsCalled_ThenReturnsEmptyList()
        {
            // Given
            var activities = new List<EndedActivity>();
            _clockMock.Setup(c => c.Now).Returns(new DateTime(2023, 1, 1, 12, 0, 0));

            // When
            var result = _sut.Generate(activities);

            // Then
            result.Should().BeEmpty();
        }

        private EndedActivity CreateActivity(DateTime start, DateTime end, string description = "Test Activity")
        {
            return new EndedActivity(start, end, new TestSystemState(description));
        }

        [Fact]
        public void GivenSingleActivity_WhenGenerateIsCalled_ThenReturnsSingleGroup()
        {
            // Given
            var now = DateTime.Now;
            var activity = CreateActivity(now.AddHours(-1), now);
            var activities = new List<EndedActivity> { activity };
            _clockMock.Setup(c => c.Now).Returns(now);

            // When
            var result = _sut.Generate(activities);

            // Then
            result.Should().ContainSingle();
            result.First().Duration.Should().Be(activity.EndDate - activity.StartDate);
        }

        [Fact]
        public void GivenMultipleIdenticalActivities_WhenGenerateIsCalled_ThenGroupsThemTogether()
        {
            // Given
            var now = DateTime.Now;
            var activity1 = CreateActivity(now.AddHours(-2), now.AddHours(-1), "Working on feature X");
            var activity2 = CreateActivity(now.AddHours(-1), now, "Working on feature X");
            var activities = new List<EndedActivity> { activity1, activity2 };
            _clockMock.Setup(c => c.Now).Returns(now);

            // When
            var result = _sut.Generate(activities);

            // Then
            result.Should().ContainSingle();
            result.First().Duration.Should().Be(TimeSpan.FromHours(2));
        }

        [Fact]
        public void GivenSimilarButNotIdenticalActivities_WhenGenerateIsCalled_ThenGroupsThemBasedOnSemanticSimilarity()
        {
            // Given
            var now = DateTime.Now;
            var activity1 = CreateActivity(now.AddHours(-2), now.AddHours(-1.5), "Implementing login feature");
            var activity2 = CreateActivity(now.AddHours(-1.5), now.AddHours(-1), "Working on login functionality");
            var activity3 = CreateActivity(now.AddHours(-1), now, "Daily standup");
            var activities = new List<EndedActivity> { activity1, activity2, activity3 };
            _clockMock.Setup(c => c.Now).Returns(now);

            // When
            var result = _sut.Generate(activities);

            // Then
            result.Should().HaveCount(2);
            result.Should().Contain(g => g.Description.Contains("login"));
            result.Should().Contain(g => g.Description.Contains("standup"));
        }

        [Fact]
        public void GivenActivitiesOnDifferentDays_WhenGenerateIsCalled_ThenGroupsThemByDate()
        {
            // Given
            var activity1 = CreateActivity(
                new DateTime(2023, 1, 1, 10, 0, 0), 
                new DateTime(2023, 1, 1, 11, 0, 0), 
                "Feature A");
                
            var activity2 = CreateActivity(
                new DateTime(2023, 1, 2, 10, 0, 0), 
                new DateTime(2023, 1, 2, 11, 0, 0), 
                "Feature A");
                
            var activities = new List<EndedActivity> { activity1, activity2 };
            _clockMock.Setup(c => c.Now).Returns(new DateTime(2023, 1, 3));

            // When
            var result = _sut.Generate(activities);

            // Then
            result.Should().HaveCount(2);
            result.Should().Contain(g => g.Date == new DateOnly(2023, 1, 1));
            result.Should().Contain(g => g.Date == new DateOnly(2023, 1, 2));
        }

        [Fact]
        public void GivenActivitiesWithDifferentDescriptions_WhenGenerateIsCalled_ThenGroupsThemBySimilarity()
        {
            // Given
            var now = DateTime.Now;
            var activities = new List<EndedActivity>
            {
                CreateActivity(now.AddHours(-4), now.AddHours(-3.5), "Fix login bug"),
                CreateActivity(now.AddHours(-3.5), now.AddHours(-3), "Fix registration bug"),
                CreateActivity(now.AddHours(-3), now.AddHours(-2), "Team sync"),
                CreateActivity(now.AddHours(-2), now.AddHours(-1), "Refactor auth service"),
                CreateActivity(now.AddHours(-1), now, "Sprint planning")
            };
            _clockMock.Setup(c => c.Now).Returns(now);

            // When
            var result = _sut.Generate(activities);

            // Then
            result.Should().HaveCount(4); // 2 coding groups (bugs + refactoring) + 2 meetings
            result.Should().Contain(g => g.Description.Contains("bug") && g.Duration.TotalHours == 1);
            result.Should().Contain(g => g.Description.Contains("refactor") && g.Duration.TotalHours == 1);
            result.Should().Contain(g => g.Description.Contains("sync") && g.Duration.TotalHours == 1);
            result.Should().Contain(g => g.Description.Contains("planning") && g.Duration.TotalHours == 1);
        }
    }
}
