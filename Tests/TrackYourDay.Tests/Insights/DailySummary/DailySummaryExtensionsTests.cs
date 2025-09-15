using FluentAssertions;
using TrackYourDay.Core.Insights.DailySummary;
using TrackYourDay.Core.SystemTrackers;
using TrackYourDay.Core.SystemTrackers.SystemStates;

namespace TrackYourDay.Tests.Insights.DailySummary
{
    public class DailySummaryExtensionsTests
    {
        [Test]
        public void Given_ActivityTrackerWithActivitiesForSpecificDate_When_GetActivitiesForDate_Then_ReturnsOnlyActivitiesFromThatDate()
        {
            // Given
            var targetDate = DateOnly.FromDateTime(DateTime.Today);
            var yesterdayDate = targetDate.AddDays(-1);
            var tomorrowDate = targetDate.AddDays(1);

            var mockActivityTracker = new Mock<ActivityTracker>();
            var allActivities = new List<EndedActivity>
            {
                // Yesterday's activities
                new(yesterdayDate.ToDateTime(TimeOnly.Parse("09:00")), yesterdayDate.ToDateTime(TimeOnly.Parse("10:00")), 
                    SystemStateFactory.FocusOnApplicationState("VS Code")),
                
                // Today's activities
                new(targetDate.ToDateTime(TimeOnly.Parse("09:00")), targetDate.ToDateTime(TimeOnly.Parse("10:00")), 
                    SystemStateFactory.FocusOnApplicationState("IntelliJ")),
                new(targetDate.ToDateTime(TimeOnly.Parse("14:00")), targetDate.ToDateTime(TimeOnly.Parse("15:00")), 
                    SystemStateFactory.FocusOnApplicationState("Chrome")),
                
                // Tomorrow's activities
                new(tomorrowDate.ToDateTime(TimeOnly.Parse("09:00")), tomorrowDate.ToDateTime(TimeOnly.Parse("10:00")), 
                    SystemStateFactory.FocusOnApplicationState("Notepad"))
            };

            mockActivityTracker.Setup(x => x.GetEndedActivities()).Returns(allActivities);

            // When
            var result = mockActivityTracker.Object.GetActivitiesForDate(targetDate);

            // Then
            result.Should().HaveCount(2);
            result.All(a => DateOnly.FromDateTime(a.StartDate) == targetDate).Should().BeTrue();
        }

        [Test]
        public void Given_DailySummaryReportWithJiraIssues_When_ToFormattedSummary_Then_ReturnsWellFormattedText()
        {
            // Given
            var date = DateOnly.FromDateTime(DateTime.Today);
            var activityPeriods = new List<ActivityPeriod>
            {
                new(DateTime.Now.AddHours(-3), DateTime.Now.AddHours(-2), "VS Code - feature branch", TimeSpan.FromHours(1)),
                new(DateTime.Now.AddHours(-1), DateTime.Now, "IntelliJ - debugging", TimeSpan.FromHours(1))
            };

            var jiraIssues = new List<JiraIssueTimeSummary>
            {
                new("PROJ-123", "Implement user authentication", TimeSpan.FromHours(2), activityPeriods)
            };

            var report = new DailySummaryReport(
                date,
                TimeSpan.FromHours(8),
                TimeSpan.FromHours(2),
                jiraIssues,
                new List<ActivityPeriod>());

            // When
            var formattedSummary = report.ToFormattedSummary();

            // Then
            formattedSummary.Should().Contain($"Daily Work Summary - {date:yyyy-MM-dd}");
            formattedSummary.Should().Contain("Total Work Time: 8h 0m");
            formattedSummary.Should().Contain("Jira-Related Time: 2h 0m");
            formattedSummary.Should().Contain("Issues Worked On: 1");
            formattedSummary.Should().Contain("[PROJ-123] Implement user authentication");
            formattedSummary.Should().Contain("Time Spent: 2h 0m");
            formattedSummary.Should().Contain("VS Code - feature branch");
            formattedSummary.Should().Contain("IntelliJ - debugging");
        }

        [Test]
        public void Given_DailySummaryReportWithNoJiraIssues_When_ToFormattedSummary_Then_ShowsNoIssuesMessage()
        {
            // Given
            var date = DateOnly.FromDateTime(DateTime.Today);
            var unassignedActivities = new List<ActivityPeriod>
            {
                new(DateTime.Now.AddHours(-2), DateTime.Now.AddHours(-1), "Notepad", TimeSpan.FromHours(1))
            };

            var report = new DailySummaryReport(
                date,
                TimeSpan.FromHours(4),
                TimeSpan.Zero,
                new List<JiraIssueTimeSummary>(),
                unassignedActivities);

            // When
            var formattedSummary = report.ToFormattedSummary();

            // Then
            formattedSummary.Should().Contain("No Jira issues detected in activities");
            formattedSummary.Should().Contain("Other Activities");
            formattedSummary.Should().Contain("Notepad");
        }

        [Test]
        public void Given_DailySummaryReportWithManyUnassignedActivities_When_ToFormattedSummary_Then_LimitsToFirst10Activities()
        {
            // Given
            var date = DateOnly.FromDateTime(DateTime.Today);
            var unassignedActivities = new List<ActivityPeriod>();
            
            // Create 15 unassigned activities
            for (int i = 0; i < 15; i++)
            {
                unassignedActivities.Add(new ActivityPeriod(
                    DateTime.Now.AddHours(-i-1), 
                    DateTime.Now.AddHours(-i), 
                    $"Activity {i}", 
                    TimeSpan.FromHours(1)));
            }

            var report = new DailySummaryReport(
                date,
                TimeSpan.FromHours(15),
                TimeSpan.Zero,
                new List<JiraIssueTimeSummary>(),
                unassignedActivities);

            // When
            var formattedSummary = report.ToFormattedSummary();

            // Then
            formattedSummary.Should().Contain("Activity 0");
            formattedSummary.Should().Contain("Activity 9");
            formattedSummary.Should().NotContain("Activity 10");
            formattedSummary.Should().Contain("... and 5 more activities");
        }

        [Test]
        public void Given_DailySummaryReportWithJiraIssues_When_ToCsv_Then_ReturnsCorrectCsvFormat()
        {
            // Given
            var date = DateOnly.FromDateTime(DateTime.Today);
            var activityPeriods = new List<ActivityPeriod>
            {
                new(DateTime.Now.AddHours(-2), DateTime.Now.AddHours(-1), "VS Code", TimeSpan.FromHours(1))
            };

            var jiraIssues = new List<JiraIssueTimeSummary>
            {
                new("PROJ-123", "Fix login bug", TimeSpan.FromMinutes(90), activityPeriods),
                new("PROJ-456", "Add new feature", TimeSpan.FromMinutes(120), activityPeriods)
            };

            var report = new DailySummaryReport(
                date,
                TimeSpan.FromHours(8),
                TimeSpan.FromMinutes(210),
                jiraIssues,
                new List<ActivityPeriod>());

            // When
            var csv = report.ToCsv();

            // Then
            csv.Should().StartWith("Date,IssueKey,IssueSummary,TimeSpent(Minutes),ActivityCount");
            csv.Should().Contain($"{date:yyyy-MM-dd},PROJ-123,\"Fix login bug\",90,1");
            csv.Should().Contain($"{date:yyyy-MM-dd},PROJ-456,\"Add new feature\",120,1");
        }

        [Test]
        public void Given_DailySummaryReportWithProductiveDay_When_GetProductivityInsights_Then_ReturnsCorrectInsights()
        {
            // Given
            var date = DateOnly.FromDateTime(DateTime.Today);
            var activityPeriods = new List<ActivityPeriod>
            {
                new(DateTime.Now.AddHours(-4), DateTime.Now.AddHours(-2), "VS Code - long session", TimeSpan.FromHours(2)),
                new(DateTime.Now.AddHours(-1), DateTime.Now, "IntelliJ - short session", TimeSpan.FromHours(1))
            };

            var jiraIssues = new List<JiraIssueTimeSummary>
            {
                new("PROJ-123", "Issue 1", TimeSpan.FromHours(2), new List<ActivityPeriod> { activityPeriods[0] }),
                new("PROJ-456", "Issue 2", TimeSpan.FromHours(1), new List<ActivityPeriod> { activityPeriods[1] })
            };

            var unassignedActivities = new List<ActivityPeriod>
            {
                new(DateTime.Now.AddHours(-5), DateTime.Now.AddHours(-4), "Email", TimeSpan.FromHours(1))
            };

            var report = new DailySummaryReport(
                date,
                TimeSpan.FromHours(4), // Total work time
                TimeSpan.FromHours(3), // Jira time
                jiraIssues,
                unassignedActivities);

            // When
            var insights = report.GetProductivityInsights();

            // Then
            insights.JiraTimePercentage.Should().Be(75.0); // 3/4 * 100
            insights.AverageTimePerIssue.Should().Be(TimeSpan.FromMinutes(90)); // 3 hours / 2 issues
            insights.LongestSession.Should().Be(activityPeriods[0]); // 2-hour session
            insights.TotalIssuesWorkedOn.Should().Be(2);
            insights.UnassignedActivitiesCount.Should().Be(1);
        }

        [Test]
        public void Given_DailySummaryReportWithNoJiraTime_When_GetProductivityInsights_Then_ReturnsZeroPercentage()
        {
            // Given
            var date = DateOnly.FromDateTime(DateTime.Today);
            var report = new DailySummaryReport(
                date,
                TimeSpan.FromHours(8),
                TimeSpan.Zero,
                new List<JiraIssueTimeSummary>(),
                new List<ActivityPeriod>());

            // When
            var insights = report.GetProductivityInsights();

            // Then
            insights.JiraTimePercentage.Should().Be(0.0);
            insights.AverageTimePerIssue.Should().Be(TimeSpan.Zero);
            insights.LongestSession.Should().BeNull();
            insights.TotalIssuesWorkedOn.Should().Be(0);
        }

        [Test]
        public void Given_ProductivityInsights_When_AccessingSummary_Then_ReturnsFormattedInsightText()
        {
            // Given
            var longestSession = new ActivityPeriod(
                DateTime.Now.AddHours(-2), 
                DateTime.Now, 
                "Long coding session", 
                TimeSpan.FromHours(2));

            var insights = new ProductivityInsights(
                JiraTimePercentage: 85.5,
                AverageTimePerIssue: TimeSpan.FromMinutes(45),
                LongestSession: longestSession,
                TotalIssuesWorkedOn: 4,
                UnassignedActivitiesCount: 2);

            // When
            var summary = insights.Summary;

            // Then
            summary.Should().Contain("85.5% of time spent on Jira issues");
            summary.Should().Contain("Average 45m per issue");
            summary.Should().Contain("Worked on 4 different issues");
            summary.Should().Contain("2 activities not linked to Jira");
        }

        [Test]
        public void Given_EmptyActivityTracker_When_GetActivitiesForDate_Then_ReturnsEmptyCollection()
        {
            // Given
            var date = DateOnly.FromDateTime(DateTime.Today);
            var mockActivityTracker = new Mock<ActivityTracker>();
            mockActivityTracker.Setup(x => x.GetEndedActivities()).Returns(new List<EndedActivity>());

            // When
            var result = mockActivityTracker.Object.GetActivitiesForDate(date);

            // Then
            result.Should().BeEmpty();
        }

        [Test]
        public void Given_ActivityPeriodWithZeroDuration_When_FormattedDuration_Then_ReturnsZeroMinutes()
        {
            // Given
            var activityPeriod = new ActivityPeriod(
                DateTime.Now,
                DateTime.Now,
                "Instant activity",
                TimeSpan.Zero);

            // When
            var formattedDuration = activityPeriod.FormattedDuration;

            // Then
            formattedDuration.Should().Be("0m");
        }

        [Test]
        public void Given_JiraIssueTimeSummaryWithZeroTime_When_FormattedTimeSpent_Then_ReturnsZeroMinutes()
        {
            // Given
            var jiraIssue = new JiraIssueTimeSummary(
                "PROJ-123",
                "Quick issue",
                TimeSpan.Zero,
                new List<ActivityPeriod>());

            // When
            var formattedTime = jiraIssue.FormattedTimeSpent;

            // Then
            formattedTime.Should().Be("0m");
        }
    }
}
