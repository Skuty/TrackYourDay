using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TrackYourDay.Core.ApplicationTrackers.Jira;
using TrackYourDay.Core.Insights.DailySummary;
using TrackYourDay.Core.Insights.Workdays;
using TrackYourDay.Core.SystemTrackers;
using TrackYourDay.Core.SystemTrackers.SystemStates;

namespace TrackYourDay.Tests.Insights.DailySummary
{
    public class DailySummaryServiceTests
    {
        private readonly Mock<IJiraActivityService> mockJiraActivityService;
        private readonly Mock<IJiraActivityCorrelationService> mockCorrelationService;
        private readonly Mock<WorkdayReadModelRepository> mockWorkdayRepository;
        private readonly Mock<ILogger<DailySummaryService>> mockLogger;
        private readonly DailySummaryService dailySummaryService;

        public DailySummaryServiceTests()
        {
            mockJiraActivityService = new Mock<IJiraActivityService>();
            mockCorrelationService = new Mock<IJiraActivityCorrelationService>();
            mockWorkdayRepository = new Mock<WorkdayReadModelRepository>();
            mockLogger = new Mock<ILogger<DailySummaryService>>();
            
            dailySummaryService = new DailySummaryService(
                mockJiraActivityService.Object,
                mockCorrelationService.Object,
                mockWorkdayRepository.Object,
                mockLogger.Object);
        }

        [Test]
        public async Task Given_ActivitiesWithJiraCorrelations_When_GenerateDailySummary_Then_ReturnsReportWithCorrectJiraIssues()
        {
            // Given
            var date = DateOnly.FromDateTime(DateTime.Today);
            var startOfDay = date.ToDateTime(TimeOnly.MinValue);
            
            var activities = new List<EndedActivity>
            {
                new(DateTime.Now.AddHours(-3), DateTime.Now.AddHours(-2), 
                    SystemStateFactory.FocusOnApplicationState("VS Code - PROJ-123")),
                new(DateTime.Now.AddHours(-2), DateTime.Now.AddHours(-1), 
                    SystemStateFactory.FocusOnApplicationState("Chrome - Jira"))
            };

            var jiraActivities = new List<JiraActivity>
            {
                new(DateTime.Now.AddHours(-2.5), "Jira Status Change - PROJ-123: Status changed | Issue: Implement feature | Time: 10:30")
            };

            var correlations = new List<JiraActivityCorrelation>
            {
                new(activities[0], "PROJ-123", CorrelationMethod.WindowTitle, 0.9),
                new(activities[1], "PROJ-123", CorrelationMethod.TimeProximity, 0.7)
            };

            mockJiraActivityService
                .Setup(x => x.GetActivitiesUpdatedAfter(startOfDay))
                .Returns(jiraActivities);

            mockCorrelationService
                .Setup(x => x.CorrelateActivitiesWithJiraIssues(activities, jiraActivities))
                .Returns(correlations);

            // When
            var report = await dailySummaryService.GenerateDailySummaryAsync(date, activities);

            // Then
            report.Should().NotBeNull();
            report.Date.Should().Be(date);
            report.JiraIssues.Should().HaveCount(1);
            
            var jiraIssue = report.JiraIssues.First();
            jiraIssue.IssueKey.Should().Be("PROJ-123");
            jiraIssue.IssueSummary.Should().Be("Implement feature");
            jiraIssue.TotalTimeSpent.Should().Be(TimeSpan.FromHours(2)); // Both activities
            jiraIssue.ActivityPeriods.Should().HaveCount(2);
        }

        [Test]
        public async Task Given_ActivitiesWithoutJiraCorrelations_When_GenerateDailySummary_Then_ReturnsReportWithUnassignedActivities()
        {
            // Given
            var date = DateOnly.FromDateTime(DateTime.Today);
            var startOfDay = date.ToDateTime(TimeOnly.MinValue);
            
            var activities = new List<EndedActivity>
            {
                new(DateTime.Now.AddHours(-2), DateTime.Now.AddHours(-1), 
                    SystemStateFactory.FocusOnApplicationState("Notepad")),
                new(DateTime.Now.AddHours(-1), DateTime.Now, 
                    SystemStateFactory.FocusOnApplicationState("Calculator"))
            };

            var jiraActivities = new List<JiraActivity>();

            var correlations = new List<JiraActivityCorrelation>
            {
                new(activities[0], null, CorrelationMethod.Manual, 0.0),
                new(activities[1], null, CorrelationMethod.Manual, 0.0)
            };

            mockJiraActivityService
                .Setup(x => x.GetActivitiesUpdatedAfter(startOfDay))
                .Returns(jiraActivities);

            mockCorrelationService
                .Setup(x => x.CorrelateActivitiesWithJiraIssues(activities, jiraActivities))
                .Returns(correlations);

            // When
            var report = await dailySummaryService.GenerateDailySummaryAsync(date, activities);

            // Then
            report.Should().NotBeNull();
            report.JiraIssues.Should().BeEmpty();
            report.UnassignedActivities.Should().HaveCount(2);
            report.TotalJiraTime.Should().Be(TimeSpan.Zero);
            report.TotalWorkTime.Should().Be(TimeSpan.FromHours(2));
        }

        [Test]
        public async Task Given_MultipleActivitiesForSameJiraIssue_When_GenerateDailySummary_Then_GroupsActivitiesCorrectly()
        {
            // Given
            var date = DateOnly.FromDateTime(DateTime.Today);
            var startOfDay = date.ToDateTime(TimeOnly.MinValue);
            
            var activities = new List<EndedActivity>
            {
                new(DateTime.Now.AddHours(-4), DateTime.Now.AddHours(-3), 
                    SystemStateFactory.FocusOnApplicationState("VS Code - PROJ-123")),
                new(DateTime.Now.AddHours(-2), DateTime.Now.AddHours(-1), 
                    SystemStateFactory.FocusOnApplicationState("IntelliJ - PROJ-123")),
                new(DateTime.Now.AddHours(-1), DateTime.Now, 
                    SystemStateFactory.FocusOnApplicationState("Chrome - PROJ-456"))
            };

            var jiraActivities = new List<JiraActivity>
            {
                new(DateTime.Now.AddHours(-3.5), "Jira Update - PROJ-123: Comment added | Issue: Fix login bug | Time: 09:30"),
                new(DateTime.Now.AddHours(-0.5), "Jira Update - PROJ-456: Status changed | Issue: Add new feature | Time: 12:30")
            };

            var correlations = new List<JiraActivityCorrelation>
            {
                new(activities[0], "PROJ-123", CorrelationMethod.WindowTitle, 0.9),
                new(activities[1], "PROJ-123", CorrelationMethod.WindowTitle, 0.9),
                new(activities[2], "PROJ-456", CorrelationMethod.WindowTitle, 0.9)
            };

            mockJiraActivityService
                .Setup(x => x.GetActivitiesUpdatedAfter(startOfDay))
                .Returns(jiraActivities);

            mockCorrelationService
                .Setup(x => x.CorrelateActivitiesWithJiraIssues(activities, jiraActivities))
                .Returns(correlations);

            // When
            var report = await dailySummaryService.GenerateDailySummaryAsync(date, activities);

            // Then
            report.Should().NotBeNull();
            report.JiraIssues.Should().HaveCount(2);
            
            var proj123Issue = report.JiraIssues.First(x => x.IssueKey == "PROJ-123");
            proj123Issue.TotalTimeSpent.Should().Be(TimeSpan.FromHours(2)); // Two 1-hour activities
            proj123Issue.ActivityPeriods.Should().HaveCount(2);
            proj123Issue.IssueSummary.Should().Be("Fix login bug");
            
            var proj456Issue = report.JiraIssues.First(x => x.IssueKey == "PROJ-456");
            proj456Issue.TotalTimeSpent.Should().Be(TimeSpan.FromHours(1));
            proj456Issue.ActivityPeriods.Should().HaveCount(1);
            proj456Issue.IssueSummary.Should().Be("Add new feature");
        }

        [Test]
        public async Task Given_JiraActivityWithoutIssueSummary_When_GenerateDailySummary_Then_UsesFallbackSummary()
        {
            // Given
            var date = DateOnly.FromDateTime(DateTime.Today);
            var startOfDay = date.ToDateTime(TimeOnly.MinValue);
            
            var activities = new List<EndedActivity>
            {
                new(DateTime.Now.AddHours(-1), DateTime.Now, 
                    SystemStateFactory.FocusOnApplicationState("VS Code - PROJ-999"))
            };

            var jiraActivities = new List<JiraActivity>
            {
                new(DateTime.Now.AddMinutes(-30), "Jira Update - PROJ-999: Status changed")
            };

            var correlations = new List<JiraActivityCorrelation>
            {
                new(activities[0], "PROJ-999", CorrelationMethod.WindowTitle, 0.9)
            };

            mockJiraActivityService
                .Setup(x => x.GetActivitiesUpdatedAfter(startOfDay))
                .Returns(jiraActivities);

            mockCorrelationService
                .Setup(x => x.CorrelateActivitiesWithJiraIssues(activities, jiraActivities))
                .Returns(correlations);

            // When
            var report = await dailySummaryService.GenerateDailySummaryAsync(date, activities);

            // Then
            report.Should().NotBeNull();
            report.JiraIssues.Should().HaveCount(1);
            
            var jiraIssue = report.JiraIssues.First();
            jiraIssue.IssueKey.Should().Be("PROJ-999");
            jiraIssue.IssueSummary.Should().Be("Issue PROJ-999"); // Fallback summary
        }

        [Test]
        public async Task Given_EmptyActivitiesList_When_GenerateDailySummary_Then_ReturnsEmptyReport()
        {
            // Given
            var date = DateOnly.FromDateTime(DateTime.Today);
            var startOfDay = date.ToDateTime(TimeOnly.MinValue);
            var activities = new List<EndedActivity>();
            var jiraActivities = new List<JiraActivity>();
            var correlations = new List<JiraActivityCorrelation>();

            mockJiraActivityService
                .Setup(x => x.GetActivitiesUpdatedAfter(startOfDay))
                .Returns(jiraActivities);

            mockCorrelationService
                .Setup(x => x.CorrelateActivitiesWithJiraIssues(activities, jiraActivities))
                .Returns(correlations);

            // When
            var report = await dailySummaryService.GenerateDailySummaryAsync(date, activities);

            // Then
            report.Should().NotBeNull();
            report.Date.Should().Be(date);
            report.JiraIssues.Should().BeEmpty();
            report.UnassignedActivities.Should().BeEmpty();
            report.TotalWorkTime.Should().Be(TimeSpan.Zero);
            report.TotalJiraTime.Should().Be(TimeSpan.Zero);
        }

        [Test]
        public async Task Given_JiraServiceThrowsException_When_GenerateDailySummary_Then_ThrowsException()
        {
            // Given
            var date = DateOnly.FromDateTime(DateTime.Today);
            var startOfDay = date.ToDateTime(TimeOnly.MinValue);
            var activities = new List<EndedActivity>();

            mockJiraActivityService
                .Setup(x => x.GetActivitiesUpdatedAfter(startOfDay))
                .Throws(new InvalidOperationException("Jira service error"));

            // When & Then
            var act = async () => await dailySummaryService.GenerateDailySummaryAsync(date, activities);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Jira service error");
        }

        [Test]
        public async Task Given_ActivitiesOrderedByTime_When_GenerateDailySummary_Then_JiraIssuesOrderedByTotalTimeDescending()
        {
            // Given
            var date = DateOnly.FromDateTime(DateTime.Today);
            var startOfDay = date.ToDateTime(TimeOnly.MinValue);
            
            var activities = new List<EndedActivity>
            {
                new(DateTime.Now.AddHours(-4), DateTime.Now.AddHours(-3.5), // 30 minutes on PROJ-111
                    SystemStateFactory.FocusOnApplicationState("VS Code - PROJ-111")),
                new(DateTime.Now.AddHours(-3), DateTime.Now.AddHours(-1), // 2 hours on PROJ-222
                    SystemStateFactory.FocusOnApplicationState("IntelliJ - PROJ-222")),
                new(DateTime.Now.AddHours(-1), DateTime.Now.AddMinutes(-15), // 45 minutes on PROJ-333
                    SystemStateFactory.FocusOnApplicationState("Chrome - PROJ-333"))
            };

            var jiraActivities = new List<JiraActivity>();

            var correlations = new List<JiraActivityCorrelation>
            {
                new(activities[0], "PROJ-111", CorrelationMethod.WindowTitle, 0.9),
                new(activities[1], "PROJ-222", CorrelationMethod.WindowTitle, 0.9),
                new(activities[2], "PROJ-333", CorrelationMethod.WindowTitle, 0.9)
            };

            mockJiraActivityService
                .Setup(x => x.GetActivitiesUpdatedAfter(startOfDay))
                .Returns(jiraActivities);

            mockCorrelationService
                .Setup(x => x.CorrelateActivitiesWithJiraIssues(activities, jiraActivities))
                .Returns(correlations);

            // When
            var report = await dailySummaryService.GenerateDailySummaryAsync(date, activities);

            // Then
            report.JiraIssues.Should().HaveCount(3);
            report.JiraIssues[0].IssueKey.Should().Be("PROJ-222"); // 2 hours (most)
            report.JiraIssues[1].IssueKey.Should().Be("PROJ-333"); // 45 minutes
            report.JiraIssues[2].IssueKey.Should().Be("PROJ-111"); // 30 minutes (least)
        }
    }
}
