using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using TrackYourDay.Core.ApplicationTrackers.Jira;
using TrackYourDay.Core.Insights.DailySummary;
using TrackYourDay.Core.SystemTrackers;
using TrackYourDay.Core.SystemTrackers.SystemStates;

namespace TrackYourDay.Tests.Insights.DailySummary
{
    public class JiraActivityCorrelationServiceTests
    {
        private readonly Mock<ILogger<JiraActivityCorrelationService>> mockLogger;
        private readonly JiraActivityCorrelationService correlationService;

        public JiraActivityCorrelationServiceTests()
        {
            mockLogger = new Mock<ILogger<JiraActivityCorrelationService>>();
            correlationService = new JiraActivityCorrelationService(mockLogger.Object);
        }

        [Fact]
        public void Given_ActivityWithJiraIssueKeyInDescription_When_CorrelatingActivities_Then_ReturnsWindowTitleCorrelation()
        {
            // Given
            var systemState = SystemStateFactory.FocusOnApplicationState("Visual Studio Code - PROJ-123-feature-branch");
            var activity = new EndedActivity(DateTime.Now.AddHours(-1), DateTime.Now, systemState);
            var activities = new List<EndedActivity> { activity };
            var jiraActivities = new List<JiraActivity>();

            // When
            var correlations = correlationService.CorrelateActivitiesWithJiraIssues(activities, jiraActivities);

            // Then
            correlations.Should().HaveCount(1);
            var correlation = correlations.First();
            correlation.DetectedIssueKey.Should().Be("PROJ-123");
            correlation.Method.Should().Be(CorrelationMethod.WindowTitle);
            correlation.ConfidenceScore.Should().Be(0.9);
            correlation.HasJiraIssue.Should().BeTrue();
        }

        [Fact]
        public void Given_ActivityWithJiraUrlInDescription_When_CorrelatingActivities_Then_ReturnsJiraWebInterfaceCorrelation()
        {
            // Given
            var systemState = SystemStateFactory.FocusOnApplicationState("Chrome - https://company.atlassian.net/jira/browse/TASK-456");
            var activity = new EndedActivity(DateTime.Now.AddHours(-1), DateTime.Now, systemState);
            var activities = new List<EndedActivity> { activity };
            var jiraActivities = new List<JiraActivity>();

            // When
            var correlations = correlationService.CorrelateActivitiesWithJiraIssues(activities, jiraActivities);

            // Then
            correlations.Should().HaveCount(1);
            var correlation = correlations.First();
            correlation.DetectedIssueKey.Should().Be("TASK-456");
            correlation.Method.Should().Be(CorrelationMethod.JiraWebInterface);
            correlation.ConfidenceScore.Should().Be(0.95);
            correlation.HasJiraIssue.Should().BeTrue();
        }

        [Fact]
        public void Given_ActivityWithNoJiraReference_When_CorrelatingActivities_Then_ReturnsManualCorrelationWithNoIssue()
        {
            // Given
            var systemState = SystemStateFactory.FocusOnApplicationState("Notepad - untitled.txt");
            var activity = new EndedActivity(DateTime.Now.AddHours(-1), DateTime.Now, systemState);
            var activities = new List<EndedActivity> { activity };
            var jiraActivities = new List<JiraActivity>();

            // When
            var correlations = correlationService.CorrelateActivitiesWithJiraIssues(activities, jiraActivities);

            // Then
            correlations.Should().HaveCount(1);
            var correlation = correlations.First();
            correlation.DetectedIssueKey.Should().BeNull();
            correlation.Method.Should().Be(CorrelationMethod.Manual);
            correlation.ConfidenceScore.Should().Be(0.0);
            correlation.HasJiraIssue.Should().BeFalse();
        }

        [Fact]
        public void Given_ActivityNearJiraActivityInTime_When_CorrelatingActivities_Then_ReturnsTimeProximityCorrelation()
        {
            // Given
            var activityTime = DateTime.Now.AddHours(-1);
            var systemState = SystemStateFactory.FocusOnApplicationState("IntelliJ IDEA");
            var activity = new EndedActivity(activityTime, activityTime.AddMinutes(30), systemState);
            var activities = new List<EndedActivity> { activity };

            var jiraActivityTime = activityTime.AddMinutes(10); // 10 minutes after activity start
            var jiraActivity = new JiraActivity(jiraActivityTime, "Jira Status Change - BUG-789: Status changed from In Progress to Done");
            var jiraActivities = new List<JiraActivity> { jiraActivity };

            // When
            var correlations = correlationService.CorrelateActivitiesWithJiraIssues(activities, jiraActivities);

            // Then
            correlations.Should().HaveCount(1);
            var correlation = correlations.First();
            correlation.DetectedIssueKey.Should().Be("BUG-789");
            correlation.Method.Should().Be(CorrelationMethod.TimeProximity);
            correlation.ConfidenceScore.Should().Be(0.7);
            correlation.HasJiraIssue.Should().BeTrue();
        }

        [Fact]
        public void Given_ActivityWithGitReference_When_CorrelatingActivities_Then_ReturnsBranchNameCorrelation()
        {
            // Given
            var systemState = SystemStateFactory.FocusOnApplicationState("Git Bash - feature/STORY-321-user-login");
            var activity = new EndedActivity(DateTime.Now.AddHours(-1), DateTime.Now, systemState);
            var activities = new List<EndedActivity> { activity };
            var jiraActivities = new List<JiraActivity>();

            // When
            var correlations = correlationService.CorrelateActivitiesWithJiraIssues(activities, jiraActivities);

            // Then
            correlations.Should().HaveCount(1);
            var correlation = correlations.First();
            correlation.DetectedIssueKey.Should().Be("STORY-321");
            correlation.Method.Should().Be(CorrelationMethod.BranchName);
            correlation.ConfidenceScore.Should().Be(0.8);
            correlation.HasJiraIssue.Should().BeTrue();
        }

        [Fact]
        public void Given_MultipleActivitiesWithDifferentCorrelationMethods_When_CorrelatingActivities_Then_ReturnsCorrectCorrelationsForEach()
        {
            // Given
            var activities = new List<EndedActivity>
            {
                new(DateTime.Now.AddHours(-3), DateTime.Now.AddHours(-2), 
                    SystemStateFactory.FocusOnApplicationState("VS Code - PROJ-111-feature")),
                new(DateTime.Now.AddHours(-2), DateTime.Now.AddHours(-1), 
                    SystemStateFactory.FocusOnApplicationState("Chrome - jira.company.com/browse/PROJ-222")),
                new(DateTime.Now.AddHours(-1), DateTime.Now, 
                    SystemStateFactory.FocusOnApplicationState("Calculator"))
            };

            var jiraActivities = new List<JiraActivity>();

            // When
            var correlations = correlationService.CorrelateActivitiesWithJiraIssues(activities, jiraActivities);

            // Then
            correlations.Should().HaveCount(3);
            
            var firstCorrelation = correlations[0];
            firstCorrelation.DetectedIssueKey.Should().Be("PROJ-111");
            firstCorrelation.Method.Should().Be(CorrelationMethod.WindowTitle);
            
            var secondCorrelation = correlations[1];
            secondCorrelation.DetectedIssueKey.Should().Be("PROJ-222");
            secondCorrelation.Method.Should().Be(CorrelationMethod.JiraWebInterface);
            
            var thirdCorrelation = correlations[2];
            thirdCorrelation.DetectedIssueKey.Should().BeNull();
            thirdCorrelation.Method.Should().Be(CorrelationMethod.Manual);
        }

        [Fact]
        public void Given_ActivityTooFarFromJiraActivity_When_CorrelatingActivities_Then_DoesNotUseTimeProximity()
        {
            // Given
            var activityTime = DateTime.Now.AddHours(-2);
            var systemState = SystemStateFactory.FocusOnApplicationState("IntelliJ IDEA");
            var activity = new EndedActivity(activityTime, activityTime.AddMinutes(30), systemState);
            var activities = new List<EndedActivity> { activity };

            var jiraActivityTime = activityTime.AddHours(1); // 1 hour after activity (outside 30min window)
            var jiraActivity = new JiraActivity(jiraActivityTime, "Jira Status Change - BUG-999: Status changed");
            var jiraActivities = new List<JiraActivity> { jiraActivity };

            // When
            var correlations = correlationService.CorrelateActivitiesWithJiraIssues(activities, jiraActivities);

            // Then
            correlations.Should().HaveCount(1);
            var correlation = correlations.First();
            correlation.DetectedIssueKey.Should().BeNull();
            correlation.Method.Should().Be(CorrelationMethod.Manual);
            correlation.ConfidenceScore.Should().Be(0.0);
        }

        [Fact]
        public void Given_ActivityWithVisualStudioAndJiraKey_When_CorrelatingActivities_Then_ReturnsBranchNameCorrelation()
        {
            // Given
            var systemState = SystemStateFactory.FocusOnApplicationState("Visual Studio - MyProject (FEATURE-555-new-api)");
            var activity = new EndedActivity(DateTime.Now.AddHours(-1), DateTime.Now, systemState);
            var activities = new List<EndedActivity> { activity };
            var jiraActivities = new List<JiraActivity>();

            // When
            var correlations = correlationService.CorrelateActivitiesWithJiraIssues(activities, jiraActivities);

            // Then
            correlations.Should().HaveCount(1);
            var correlation = correlations.First();
            correlation.DetectedIssueKey.Should().Be("FEATURE-555");
            correlation.Method.Should().Be(CorrelationMethod.BranchName);
            correlation.ConfidenceScore.Should().Be(0.8);
        }

        [Fact]
        public void Given_EmptyActivitiesList_When_CorrelatingActivities_Then_ReturnsEmptyCorrelationsList()
        {
            // Given
            var activities = new List<EndedActivity>();
            var jiraActivities = new List<JiraActivity>();

            // When
            var correlations = correlationService.CorrelateActivitiesWithJiraIssues(activities, jiraActivities);

            // Then
            correlations.Should().BeEmpty();
        }

        [Fact]
        public void Given_ActivityWithMultipleJiraKeysInDescription_When_CorrelatingActivities_Then_ReturnsFirstFoundKey()
        {
            // Given
            var systemState = SystemStateFactory.FocusOnApplicationState("VS Code - PROJ-123 related to BUG-456");
            var activity = new EndedActivity(DateTime.Now.AddHours(-1), DateTime.Now, systemState);
            var activities = new List<EndedActivity> { activity };
            var jiraActivities = new List<JiraActivity>();

            // When
            var correlations = correlationService.CorrelateActivitiesWithJiraIssues(activities, jiraActivities);

            // Then
            correlations.Should().HaveCount(1);
            var correlation = correlations.First();
            correlation.DetectedIssueKey.Should().Be("PROJ-123");
            correlation.Method.Should().Be(CorrelationMethod.WindowTitle);
        }
    }
}
