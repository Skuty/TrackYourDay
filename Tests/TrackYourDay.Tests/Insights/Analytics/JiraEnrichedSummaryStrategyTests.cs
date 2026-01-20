using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TrackYourDay.Core;
using TrackYourDay.Core.ApplicationTrackers.Jira;
using TrackYourDay.Core.ApplicationTrackers.Persistence;
using TrackYourDay.Core.Insights.Analytics;
using TrackYourDay.Core.SystemTrackers;
using TrackYourDay.Tests.TestHelpers;

namespace TrackYourDay.Tests.Insights.Analytics
{
    public class JiraEnrichedSummaryStrategyTests : IDisposable
    {
        private readonly Mock<ILogger<JiraEnrichedSummaryStrategy>> _loggerMock;
        private readonly Mock<IJiraActivityService> _jiraActivityServiceMock;
        private readonly Mock<IJiraActivityRepository> _repositoryMock;
        private readonly Mock<IJiraSettingsService> _settingsServiceMock;
        private readonly Mock<IClock> _clockMock;
        private readonly Mock<ILogger<JiraTracker>> _trackerLoggerMock;
        private readonly JiraTracker _jiraTracker;
        private JiraEnrichedSummaryStrategy _sut;

        public JiraEnrichedSummaryStrategyTests()
        {
            _loggerMock = new Mock<ILogger<JiraEnrichedSummaryStrategy>>();
            _clockMock = new Mock<IClock>();
            _clockMock.Setup(c => c.Now).Returns(DateTime.Now);
            _jiraActivityServiceMock = new Mock<IJiraActivityService>();
            _repositoryMock = new Mock<IJiraActivityRepository>();
            _settingsServiceMock = new Mock<IJiraSettingsService>();
            _trackerLoggerMock = new Mock<ILogger<JiraTracker>>();
            
            _jiraTracker = new JiraTracker(
                _jiraActivityServiceMock.Object,
                _repositoryMock.Object,
                _settingsServiceMock.Object,
                _trackerLoggerMock.Object);
            
            _sut = new JiraEnrichedSummaryStrategy(_jiraTracker, _loggerMock.Object);
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
            _repositoryMock.Setup(r => r.GetActivitiesAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<JiraActivity>());

            // When
            var result = _sut.Generate(activities);

            // Then
            result.Should().BeEmpty();
        }

        [Fact]
        public void GivenActivitiesWithJiraKeys_WhenGenerateIsCalled_ThenGroupsByJiraKey()
        {
            // Given
            var now = DateTime.Now;
            var activities = new List<EndedActivity>
            {
                CreateActivity(now.AddHours(-2), now.AddHours(-1.5), "Working on PROJ-123 login feature"),
                CreateActivity(now.AddHours(-1.5), now.AddHours(-1), "PROJ-123 testing"),
                CreateActivity(now.AddHours(-1), now.AddHours(-0.5), "Implementing PROJ-456 dashboard"),
                CreateActivity(now.AddHours(-0.5), now, "PROJ-456 code review")
            };

            var jiraActivities = new List<JiraActivity>
            {
                new() { UpstreamId = "jira-proj123", OccurrenceDate = now.AddHours(-2), Description = "Jira Issue Updated - PROJ-123: Login Authentication | Updated: 2023-01-01 10:00 | Issue ID: 1" },
                new() { UpstreamId = "jira-proj456", OccurrenceDate = now.AddHours(-1), Description = "Jira Issue Updated - PROJ-456: User Dashboard | Updated: 2023-01-01 11:00 | Issue ID: 2" }
            };

            _repositoryMock.Setup(r => r.GetActivitiesAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(jiraActivities);

            // When
            var result = _sut.Generate(activities);

            // Then
            result.Should().HaveCount(2);
            result.Should().Contain(g => g.Description.Contains("PROJ-123") && g.Duration == TimeSpan.FromHours(1));
            result.Should().Contain(g => g.Description.Contains("PROJ-456") && g.Duration == TimeSpan.FromHours(1));
        }

        [Fact]
        public void GivenActivitiesWithJiraKeys_WhenGenerateIsCalled_ThenEnrichesWithJiraIssueSummary()
        {
            // Given
            var now = DateTime.Now;
            var activities = new List<EndedActivity>
            {
                CreateActivity(now.AddHours(-1), now, "Working on PROJ-123")
            };

            var jiraActivities = new List<JiraActivity>
            {
                new() { UpstreamId = "jira-proj123-v2", OccurrenceDate = now.AddHours(-1), Description = "Jira Issue Updated - PROJ-123: Login Authentication Feature | Updated: 2023-01-01 10:00 | Issue ID: 1" }
            };

            _repositoryMock.Setup(r => r.GetActivitiesAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(jiraActivities);

            // When
            var result = _sut.Generate(activities);

            // Then
            result.Should().ContainSingle();
            result.First().Description.Should().Contain("PROJ-123");
            result.First().Description.Should().Contain("Login Authentication Feature");
        }

        [Fact]
        public void GivenActivitiesWithoutJiraKeys_WhenGenerateIsCalled_ThenAttemptsSemanticMatching()
        {
            // Given
            var now = DateTime.Now;
            var activities = new List<EndedActivity>
            {
                CreateActivity(now.AddMinutes(-20), now.AddMinutes(-10), "Working on login authentication"),
                CreateActivity(now.AddMinutes(-10), now, "Implementing authentication feature")
            };

            var jiraActivities = new List<JiraActivity>
            {
                new() { UpstreamId = "jira-proj123-v3", OccurrenceDate = now.AddMinutes(-25), Description = "Jira Issue Updated - PROJ-123: Login Authentication Feature | Updated: 2023-01-01 09:00 | Issue ID: 1" }
            };

            _repositoryMock.Setup(r => r.GetActivitiesAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(jiraActivities);

            // When
            var result = _sut.Generate(activities);

            // Then
            // Both activities should be matched to PROJ-123 based on semantic similarity and temporal proximity
            result.Should().ContainSingle();
            result.First().Description.Should().Contain("PROJ-123");
            result.First().Duration.Should().Be(TimeSpan.FromMinutes(20));
        }

        [Fact]
        public void GivenActivitiesWithoutJiraMatch_WhenGenerateIsCalled_ThenGroupsUnderOriginalDescription()
        {
            // Given
            var now = DateTime.Now;
            var activities = new List<EndedActivity>
            {
                CreateActivity(now.AddHours(-1), now, "Random task without Jira")
            };

            _repositoryMock.Setup(r => r.GetActivitiesAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<JiraActivity>());

            // When
            var result = _sut.Generate(activities);

            // Then
            result.Should().ContainSingle();
            result.First().Description.Should().Be("Random task without Jira");
        }

        [Fact]
        public void GivenMultipleActivitiesOnDifferentDays_WhenGenerateIsCalled_ThenGroupsByDateAndJiraKey()
        {
            // Given
            var activity1 = CreateActivity(
                new DateTime(2023, 1, 1, 10, 0, 0),
                new DateTime(2023, 1, 1, 11, 0, 0),
                "Working on PROJ-123");

            var activity2 = CreateActivity(
                new DateTime(2023, 1, 2, 10, 0, 0),
                new DateTime(2023, 1, 2, 11, 0, 0),
                "Working on PROJ-123");

            var activities = new List<EndedActivity> { activity1, activity2 };

            var jiraActivities = new List<JiraActivity>
            {
                new() { UpstreamId = "jira-proj123-day1", OccurrenceDate = new DateTime(2023, 1, 1, 9, 0, 0), Description = "Jira Issue Updated - PROJ-123: Feature | Updated: 2023-01-01 09:00 | Issue ID: 1" },
                new() { UpstreamId = "jira-proj123-day2", OccurrenceDate = new DateTime(2023, 1, 2, 9, 0, 0), Description = "Jira Issue Updated - PROJ-123: Feature | Updated: 2023-01-02 09:00 | Issue ID: 1" }
            };

            _repositoryMock.Setup(r => r.GetActivitiesAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(jiraActivities);

            // When
            var result = _sut.Generate(activities);

            // Then
            result.Should().HaveCount(2);
            result.Should().Contain(g => g.Date == new DateOnly(2023, 1, 1));
            result.Should().Contain(g => g.Date == new DateOnly(2023, 1, 2));
        }

        [Fact]
        public void GivenMixedActivities_WhenGenerateIsCalled_ThenGroupsCorrectly()
        {
            // Given
            var now = DateTime.Now;
            var activities = new List<EndedActivity>
            {
                CreateActivity(now.AddHours(-3), now.AddHours(-2.5), "PROJ-123 implementation"),
                CreateActivity(now.AddHours(-2.5), now.AddHours(-2), "Code review for login"),
                CreateActivity(now.AddHours(-2), now.AddHours(-1.5), "PROJ-456 bug fix"),
                CreateActivity(now.AddHours(-1.5), now.AddHours(-1), "Team standup meeting"),
                CreateActivity(now.AddHours(-1), now, "Documentation update")
            };

            var jiraActivities = new List<JiraActivity>
            {
                new() { UpstreamId = "jira-proj123-login", OccurrenceDate = now.AddHours(-3), Description = "Jira Issue Updated - PROJ-123: Login Feature | Updated: 2023-01-01 09:00 | Issue ID: 1" },
                new() { UpstreamId = "jira-proj456-bug", OccurrenceDate = now.AddHours(-2), Description = "Jira Issue Updated - PROJ-456: Critical Bug | Updated: 2023-01-01 10:00 | Issue ID: 2" }
            };

            _repositoryMock.Setup(r => r.GetActivitiesAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(jiraActivities);

            // When
            var result = _sut.Generate(activities);

            // Then
            result.Should().HaveCountGreaterOrEqualTo(3);
            result.Should().Contain(g => g.Description.Contains("PROJ-123"));
            result.Should().Contain(g => g.Description.Contains("PROJ-456"));
        }

        [Fact]
        public void GivenActivitiesCloseInTimeToJiraUpdate_WhenGenerateIsCalled_ThenMatchesTemporally()
        {
            // Given
            var now = DateTime.Now;
            // Activity starts 10 minutes after Jira update
            var activities = new List<EndedActivity>
            {
                CreateActivity(now.AddMinutes(-20), now.AddMinutes(-10), "Working on authentication")
            };

            var jiraActivities = new List<JiraActivity>
            {
                new() { UpstreamId = "jira-proj999-auth", OccurrenceDate = now.AddMinutes(-30), Description = "Jira Issue Updated - PROJ-999: Authentication System | Updated: 2023-01-01 10:00 | Issue ID: 1" }
            };

            _repositoryMock.Setup(r => r.GetActivitiesAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(jiraActivities);

            // When
            var result = _sut.Generate(activities);

            // Then
            result.Should().ContainSingle();
            result.First().Description.Should().Contain("PROJ-999");
        }

        [Fact]
        public void GivenStrategyName_WhenAccessed_ThenReturnsCorrectName()
        {
            // When
            var strategyName = _sut.StrategyName;

            // Then
            strategyName.Should().Be("Jira-Enriched Activity Groups");
        }
    }
}
