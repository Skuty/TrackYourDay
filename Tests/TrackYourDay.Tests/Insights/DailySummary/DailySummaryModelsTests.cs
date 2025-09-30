using FluentAssertions;
using Xunit;
using TrackYourDay.Core.Insights.DailySummary;
using TrackYourDay.Core.SystemTrackers;
using TrackYourDay.Core.SystemTrackers.SystemStates;

namespace TrackYourDay.Tests.Insights.DailySummary
{
    public class DailySummaryModelsTests
    {
        [Fact]
        public void Given_JiraIssueTimeSummary_When_TotalTimeSpentIsHours_Then_FormattedTimeSpentShowsHoursAndMinutes()
        {
            // Given
            var totalTimeSpent = TimeSpan.FromMinutes(125); // 2h 5m
            var jiraIssue = new JiraIssueTimeSummary(
                "PROJ-123",
                "Test Issue",
                totalTimeSpent,
                new List<ActivityPeriod>());

            // When
            var formattedTime = jiraIssue.FormattedTimeSpent;

            // Then
            formattedTime.Should().Be("2h 5m");
        }

        [Fact]
        public void Given_JiraIssueTimeSummary_When_TotalTimeSpentIsMinutesOnly_Then_FormattedTimeSpentShowsMinutesOnly()
        {
            // Given
            var totalTimeSpent = TimeSpan.FromMinutes(45);
            var jiraIssue = new JiraIssueTimeSummary(
                "PROJ-123",
                "Test Issue",
                totalTimeSpent,
                new List<ActivityPeriod>());

            // When
            var formattedTime = jiraIssue.FormattedTimeSpent;

            // Then
            formattedTime.Should().Be("45m");
        }

        [Fact]
        public void Given_ActivityPeriod_When_CreatedFromEndedActivity_Then_PropertiesAreCorrectlyMapped()
        {
            // Given
            var startDate = DateTime.Now.AddHours(-2);
            var endDate = DateTime.Now.AddHours(-1);
            var systemState = SystemStateFactory.FocusOnApplicationState("Visual Studio Code");
            var endedActivity = new EndedActivity(startDate, endDate, systemState);

            // When
            var activityPeriod = ActivityPeriod.FromEndedActivity(endedActivity);

            // Then
            activityPeriod.StartTime.Should().Be(startDate);
            activityPeriod.EndTime.Should().Be(endDate);
            activityPeriod.Duration.Should().Be(TimeSpan.FromHours(1));
            activityPeriod.ActivityDescription.Should().Be(systemState.ActivityDescription);
        }

        [Fact]
        public void Given_ActivityPeriod_When_DurationIsFormatted_Then_ReturnsCorrectFormat()
        {
            // Given
            var activityPeriod = new ActivityPeriod(
                DateTime.Now.AddMinutes(-75),
                DateTime.Now,
                "Test Activity",
                TimeSpan.FromMinutes(75));

            // When
            var formattedDuration = activityPeriod.FormattedDuration;

            // Then
            formattedDuration.Should().Be("1h 15m");
        }

        [Fact]
        public void Given_DailySummaryReport_When_HasJiraIssues_Then_TotalJiraIssuesWorkedOnReturnsCorrectCount()
        {
            // Given
            var jiraIssues = new List<JiraIssueTimeSummary>
            {
                new("PROJ-123", "Issue 1", TimeSpan.FromHours(2), new List<ActivityPeriod>()),
                new("PROJ-124", "Issue 2", TimeSpan.FromHours(1), new List<ActivityPeriod>()),
                new("PROJ-125", "Issue 3", TimeSpan.FromMinutes(30), new List<ActivityPeriod>())
            };

            var report = new DailySummaryReport(
                DateOnly.FromDateTime(DateTime.Today),
                TimeSpan.FromHours(8),
                TimeSpan.FromHours(3.5),
                jiraIssues,
                new List<ActivityPeriod>());

            // When
            var totalIssues = report.TotalJiraIssuesWorkedOn;

            // Then
            totalIssues.Should().Be(3);
        }

        [Fact]
        public void Given_DailySummaryReport_When_HasJiraIssues_Then_MostWorkedOnIssueReturnsIssueWithLongestTime()
        {
            // Given
            var issue1 = new JiraIssueTimeSummary("PROJ-123", "Issue 1", TimeSpan.FromHours(1), new List<ActivityPeriod>());
            var issue2 = new JiraIssueTimeSummary("PROJ-124", "Issue 2", TimeSpan.FromHours(3), new List<ActivityPeriod>());
            var issue3 = new JiraIssueTimeSummary("PROJ-125", "Issue 3", TimeSpan.FromMinutes(30), new List<ActivityPeriod>());

            var jiraIssues = new List<JiraIssueTimeSummary> { issue1, issue2, issue3 };

            var report = new DailySummaryReport(
                DateOnly.FromDateTime(DateTime.Today),
                TimeSpan.FromHours(8),
                TimeSpan.FromHours(4.5),
                jiraIssues,
                new List<ActivityPeriod>());

            // When
            var mostWorkedOnIssue = report.MostWorkedOnIssue;

            // Then
            mostWorkedOnIssue.Should().Be(issue2);
            mostWorkedOnIssue!.IssueKey.Should().Be("PROJ-124");
            mostWorkedOnIssue.TotalTimeSpent.Should().Be(TimeSpan.FromHours(3));
        }

        [Fact]
        public void Given_DailySummaryReport_When_NoJiraIssues_Then_MostWorkedOnIssueReturnsNull()
        {
            // Given
            var report = new DailySummaryReport(
                DateOnly.FromDateTime(DateTime.Today),
                TimeSpan.FromHours(8),
                TimeSpan.Zero,
                new List<JiraIssueTimeSummary>(),
                new List<ActivityPeriod>());

            // When
            var mostWorkedOnIssue = report.MostWorkedOnIssue;

            // Then
            mostWorkedOnIssue.Should().BeNull();
        }

        [Fact]
        public void Given_JiraActivityCorrelation_When_DetectedIssueKeyIsNotEmpty_Then_HasJiraIssueReturnsTrue()
        {
            // Given
            var systemState = SystemStateFactory.FocusOnApplicationState("Visual Studio Code");
            var endedActivity = new EndedActivity(DateTime.Now.AddHours(-1), DateTime.Now, systemState);
            var correlation = new JiraActivityCorrelation(
                endedActivity,
                "PROJ-123",
                CorrelationMethod.WindowTitle,
                0.9);

            // When
            var hasJiraIssue = correlation.HasJiraIssue;

            // Then
            hasJiraIssue.Should().BeTrue();
        }

        [Fact]
        public void Given_JiraActivityCorrelation_When_DetectedIssueKeyIsNull_Then_HasJiraIssueReturnsFalse()
        {
            // Given
            var systemState = SystemStateFactory.FocusOnApplicationState("Notepad");
            var endedActivity = new EndedActivity(DateTime.Now.AddHours(-1), DateTime.Now, systemState);
            var correlation = new JiraActivityCorrelation(
                endedActivity,
                null,
                CorrelationMethod.Manual,
                0.0);

            // When
            var hasJiraIssue = correlation.HasJiraIssue;

            // Then
            hasJiraIssue.Should().BeFalse();
        }

        [Fact]
        public void Given_JiraActivityCorrelation_When_DetectedIssueKeyIsEmpty_Then_HasJiraIssueReturnsFalse()
        {
            // Given
            var systemState = SystemStateFactory.FocusOnApplicationState("Notepad");
            var endedActivity = new EndedActivity(DateTime.Now.AddHours(-1), DateTime.Now, systemState);
            var correlation = new JiraActivityCorrelation(
                endedActivity,
                string.Empty,
                CorrelationMethod.Manual,
                0.0);

            // When
            var hasJiraIssue = correlation.HasJiraIssue;

            // Then
            hasJiraIssue.Should().BeFalse();
        }

        [Fact]
        public void Given_DailySummaryReport_When_FormattedTimes_Then_ReturnsCorrectlyFormattedStrings()
        {
            // Given
            var report = new DailySummaryReport(
                DateOnly.FromDateTime(DateTime.Today),
                TimeSpan.FromMinutes(480), // 8h
                TimeSpan.FromMinutes(315), // 5h 15m
                new List<JiraIssueTimeSummary>(),
                new List<ActivityPeriod>());

            // When
            var formattedTotalWorkTime = report.FormattedTotalWorkTime;
            var formattedTotalJiraTime = report.FormattedTotalJiraTime;

            // Then
            formattedTotalWorkTime.Should().Be("8h 0m");
            formattedTotalJiraTime.Should().Be("5h 15m");
        }
    }
}
