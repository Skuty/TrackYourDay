using FluentAssertions;

using Moq;

using TrackYourDay.Core;
using TrackYourDay.Core.ApplicationTrackers.Jira;

namespace TrackYourDay.Tests.ApplicationTrackers.Jira
{
    public class JiraTrackerTests
    {
        private readonly Mock<IClock> clockMock;
        private readonly Mock<IJiraRestApiClient> jiraRestApiClientMock;
        private readonly JiraActivityService jiraActivityService;
        private readonly JiraTracker jiraTracker;

        public JiraTrackerTests()
        {
            this.clockMock = new Mock<IClock>();
            this.jiraRestApiClientMock = new Mock<IJiraRestApiClient>();
            this.jiraActivityService = new JiraActivityService(jiraRestApiClientMock.Object, null);
            this.jiraTracker = new JiraTracker(this.jiraActivityService, clockMock.Object);
        }

        [Fact]
        public async Task GivenNoActivitiesWereTrackedAndThereAreJiraActivitiesWaiting_WhenGetingJiraActivities_ThenTrackerShouldReturnOnlyNewActivities()
        {
            // Given
            var newJiraIssues = new List<JiraIssueResponse>
            {
                new JiraIssueResponse("KEY-1", "ID-1", new JiraIssueFieldsResponse("Summary 1", new DateTime(2000, 01, 01, 13, 00, 00), null, null, null, null, null, null, null), null),
                new JiraIssueResponse("KEY-2", "ID-2", new JiraIssueFieldsResponse("Summary 2", new DateTime(2000, 01, 01, 14, 00, 00), null, null, null, null, null, null, null), null),
            };

            this.jiraRestApiClientMock.Setup(client => client.GetUserIssues(It.IsAny<JiraUser>(), It.IsAny<DateTime>()))
                .ReturnsAsync(newJiraIssues);
            this.jiraRestApiClientMock.Setup(client => client.GetIssueWorklogs(It.IsAny<string>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<JiraWorklogResponse>());

            this.clockMock.Setup(clock => clock.Now).Returns(new DateTime(2000, 01, 01, 15, 00, 00));

            // When
            await this.jiraTracker.RecognizeActivity(); //TODO: This probably should be exposed somewhere in act or arrange?
            var result = await this.jiraTracker.GetJiraActivities();

            // Then
            result.Should().NotBeNull();
            // With enhanced tracking, issues without changelog or worklogs produce no activities
            result.Should().HaveCount(0);
        }

        [Fact]
        public async Task GivenTwoActivitiesWereTrackedAndThereTwoMoreActivitiesWaiting_WhenGetingJiraActivities_ThenTrackerShouldReturnFourActivities()
        {
            // Given
            var oldJiraIssues = new List<JiraIssueResponse>
            {
                new JiraIssueResponse("KEY-1", "ID-1", new JiraIssueFieldsResponse("Summary 1", new DateTime(2000, 01, 01, 13, 00, 00), null, null, null, null, null, null, null), null),
                new JiraIssueResponse("KEY-2", "ID-2", new JiraIssueFieldsResponse("Summary 2", new DateTime(2000, 01, 01, 14, 00, 00), null, null, null, null, null, null, null), null),
            };

            this.jiraRestApiClientMock.Setup(client => client.GetUserIssues(It.IsAny<JiraUser>(), It.IsAny<DateTime>()))
                .ReturnsAsync(oldJiraIssues);
            this.jiraRestApiClientMock.Setup(client => client.GetIssueWorklogs(It.IsAny<string>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<JiraWorklogResponse>());
            this.clockMock.Setup(clock => clock.Now).Returns(new DateTime(2000, 01, 01, 15, 00, 00));
            await this.jiraTracker.RecognizeActivity();

            var newJiraIssues = new List<JiraIssueResponse>
            {
                new JiraIssueResponse("KEY-3", "ID-3", new JiraIssueFieldsResponse("Summary 3", new DateTime(2000, 01, 01, 17, 00, 00), null, null, null, null, null, null, null), null),
                new JiraIssueResponse("KEY-4", "ID-4", new JiraIssueFieldsResponse("Summary 4", new DateTime(2000, 01, 01, 18, 00, 00), null, null, null, null, null, null, null), null),
            };

            this.clockMock.Setup(clock => clock.Now).Returns(new DateTime(2000, 01, 01, 16, 00, 00));

            // When
            await this.jiraTracker.RecognizeActivity();
            var result = await this.jiraTracker.GetJiraActivities();

            // Then
            result.Should().NotBeNull();
            // With enhanced tracking, issues without changelog or worklogs produce no activities
            result.Should().HaveCount(0);
        }
    }
}