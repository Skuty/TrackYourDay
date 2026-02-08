using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TrackYourDay.Core;
using TrackYourDay.Core.ApplicationTrackers.Jira;
using TrackYourDay.Core.ApplicationTrackers.Persistence;

namespace TrackYourDay.Tests.ApplicationTrackers.Jira
{
    public class JiraCurrentStateServiceTests
    {
        private readonly Mock<IJiraRestApiClient> _restApiClientMock;
        private readonly Mock<IJiraIssueRepository> _issueRepositoryMock;
        private readonly Mock<IJiraSettingsService> _jiraSettingsMock;
        private readonly Mock<IClock> _clockMock;
        private readonly Mock<ILogger<JiraCurrentStateService>> _loggerMock;
        private readonly JiraCurrentStateService _sut;

        public JiraCurrentStateServiceTests()
        {
            _restApiClientMock = new Mock<IJiraRestApiClient>();
            _issueRepositoryMock = new Mock<IJiraIssueRepository>();
            _jiraSettingsMock = new Mock<IJiraSettingsService>();
            _clockMock = new Mock<IClock>();
            _loggerMock = new Mock<ILogger<JiraCurrentStateService>>();

            _jiraSettingsMock.Setup(s => s.GetSettings())
                .Returns(new JiraSettings 
                { 
                    ApiUrl = "https://test.atlassian.net/rest/api/3",
                    ApiKey = "test-key"
                });

            _sut = new JiraCurrentStateService(
                _restApiClientMock.Object,
                _issueRepositoryMock.Object,
                _jiraSettingsMock.Object,
                _clockMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task GivenValidUser_WhenSyncStateFromRemoteService_ThenFetchesAndUpdatesIssues()
        {
            // Given
            var currentUser = new JiraUser("testuser", "Test User", "account123");
            var currentTime = new DateTime(2026, 1, 18, 12, 0, 0, DateTimeKind.Utc);
            var lookbackDate = currentTime.AddDays(-7);

            var issueResponses = new List<JiraIssueResponse>
            {
                new JiraIssueResponse(
                    "PROJ-123",
                    "10001",
                    new JiraIssueFieldsResponse(
                        "Test issue",
                        new DateTimeOffset(2026, 1, 18, 10, 0, 0, TimeSpan.Zero),
                        new DateTimeOffset(2026, 1, 10, 8, 0, 0, TimeSpan.Zero),
                        new JiraStatusResponse("In Progress"),
                        new JiraUserResponse("Test User"),
                        null,
                        new JiraIssueTypeResponse("Task", false),
                        new JiraProjectResponse("PROJ", "Project"),
                        null),
                    null)
            };

            _clockMock.Setup(c => c.Now).Returns(currentTime);
            _restApiClientMock.Setup(c => c.GetCurrentUser()).ReturnsAsync(currentUser);
            _restApiClientMock.Setup(c => c.GetUserIssues(currentUser, lookbackDate)).ReturnsAsync(issueResponses);
            _issueRepositoryMock.Setup(r => r.UpdateCurrentStateAsync(
                It.IsAny<IEnumerable<JiraIssueState>>(),
                It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            // When
            await _sut.SyncStateFromRemoteService(CancellationToken.None);

            // Then
            _restApiClientMock.Verify(c => c.GetCurrentUser(), Times.Once);
            _restApiClientMock.Verify(c => c.GetUserIssues(currentUser, lookbackDate), Times.Once);
            _issueRepositoryMock.Verify(r => r.UpdateCurrentStateAsync(
                It.Is<IEnumerable<JiraIssueState>>(issues => 
                    issues.Count() == 1 && 
                    issues.First().Key == "PROJ-123" &&
                    issues.First().BrowseUrl == "https://test.atlassian.net/browse/PROJ-123"),
                CancellationToken.None), Times.Once);
        }

        [Fact]
        public async Task GivenNoIssues_WhenSyncStateFromRemoteService_ThenUpdatesRepository()
        {
            // Given
            var currentUser = new JiraUser("testuser", "Test User", "account123");
            var currentTime = new DateTime(2026, 1, 18, 12, 0, 0, DateTimeKind.Utc);

            _clockMock.Setup(c => c.Now).Returns(currentTime);
            _restApiClientMock.Setup(c => c.GetCurrentUser()).ReturnsAsync(currentUser);
            _restApiClientMock.Setup(c => c.GetUserIssues(It.IsAny<JiraUser>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<JiraIssueResponse>());
            _issueRepositoryMock.Setup(r => r.UpdateCurrentStateAsync(
                It.IsAny<IEnumerable<JiraIssueState>>(),
                It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            // When
            await _sut.SyncStateFromRemoteService(CancellationToken.None);

            // Then
            _issueRepositoryMock.Verify(r => r.UpdateCurrentStateAsync(
                It.Is<IEnumerable<JiraIssueState>>(issues => issues.Count() == 0),
                CancellationToken.None), Times.Once);
        }

        [Fact]
        public async Task GivenMultipleCalls_WhenSyncStateFromRemoteService_ThenReusesCurrentUser()
        {
            // Given
            var currentUser = new JiraUser("testuser", "Test User", "account123");
            var currentTime = new DateTime(2026, 1, 18, 12, 0, 0, DateTimeKind.Utc);

            _clockMock.Setup(c => c.Now).Returns(currentTime);
            _restApiClientMock.Setup(c => c.GetCurrentUser()).ReturnsAsync(currentUser);
            _restApiClientMock.Setup(c => c.GetUserIssues(It.IsAny<JiraUser>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<JiraIssueResponse>());
            _issueRepositoryMock.Setup(r => r.UpdateCurrentStateAsync(
                It.IsAny<IEnumerable<JiraIssueState>>(),
                It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            // When
            await _sut.SyncStateFromRemoteService(CancellationToken.None);
            await _sut.SyncStateFromRemoteService(CancellationToken.None);

            // Then
            _restApiClientMock.Verify(c => c.GetCurrentUser(), Times.Once);
        }

        [Fact]
        public async Task GivenIssueResponse_WhenSyncStateFromRemoteService_ThenMapsAllFields()
        {
            // Given
            var currentUser = new JiraUser("testuser", "Test User", "account123");
            var currentTime = new DateTime(2026, 1, 18, 12, 0, 0, DateTimeKind.Utc);
            var updatedTime = new DateTimeOffset(2026, 1, 18, 10, 0, 0, TimeSpan.Zero);
            var createdTime = new DateTimeOffset(2026, 1, 10, 8, 0, 0, TimeSpan.Zero);

            var issueResponse = new JiraIssueResponse(
                "PROJ-456",
                "10002",
                new JiraIssueFieldsResponse(
                    "Complex issue",
                    updatedTime,
                    createdTime,
                    new JiraStatusResponse("Done"),
                    new JiraUserResponse("John Doe"),
                    null,
                    new JiraIssueTypeResponse("Bug", false),
                    new JiraProjectResponse("PROJ", "Project Name"),
                    null),
                null);

            _clockMock.Setup(c => c.Now).Returns(currentTime);
            _restApiClientMock.Setup(c => c.GetCurrentUser()).ReturnsAsync(currentUser);
            _restApiClientMock.Setup(c => c.GetUserIssues(It.IsAny<JiraUser>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<JiraIssueResponse> { issueResponse });

            JiraIssueState? capturedState = null;
            _issueRepositoryMock.Setup(r => r.UpdateCurrentStateAsync(
                It.IsAny<IEnumerable<JiraIssueState>>(),
                It.IsAny<CancellationToken>()))
                .Callback<IEnumerable<JiraIssueState>, CancellationToken>((states, _) =>
                {
                    capturedState = states.First();
                })
                .Returns(Task.CompletedTask);

            // When
            await _sut.SyncStateFromRemoteService(CancellationToken.None);

            // Then
            capturedState.Should().NotBeNull();
            capturedState!.Key.Should().Be("PROJ-456");
            capturedState.Id.Should().Be("10002");
            capturedState.Summary.Should().Be("Complex issue");
            capturedState.Status.Should().Be("Done");
            capturedState.IssueType.Should().Be("Bug");
            capturedState.ProjectKey.Should().Be("PROJ");
            capturedState.Updated.Should().Be(updatedTime);
            capturedState.Created.Should().Be(createdTime);
            capturedState.AssigneeDisplayName.Should().Be("John Doe");
        }

        [Fact]
        public async Task GivenIssueWithNullFields_WhenSyncStateFromRemoteService_ThenUsesDefaults()
        {
            // Given
            var currentUser = new JiraUser("testuser", "Test User", "account123");
            var currentTime = new DateTime(2026, 1, 18, 12, 0, 0, DateTimeKind.Utc);
            var updatedTime = new DateTimeOffset(2026, 1, 18, 10, 0, 0, TimeSpan.Zero);

            var issueResponse = new JiraIssueResponse(
                "PROJ-789",
                "10003",
                new JiraIssueFieldsResponse(
                    null,
                    updatedTime,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null),
                null);

            _clockMock.Setup(c => c.Now).Returns(currentTime);
            _restApiClientMock.Setup(c => c.GetCurrentUser()).ReturnsAsync(currentUser);
            _restApiClientMock.Setup(c => c.GetUserIssues(It.IsAny<JiraUser>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<JiraIssueResponse> { issueResponse });

            JiraIssueState? capturedState = null;
            _issueRepositoryMock.Setup(r => r.UpdateCurrentStateAsync(
                It.IsAny<IEnumerable<JiraIssueState>>(),
                It.IsAny<CancellationToken>()))
                .Callback<IEnumerable<JiraIssueState>, CancellationToken>((states, _) =>
                {
                    capturedState = states.First();
                })
                .Returns(Task.CompletedTask);

            // When
            await _sut.SyncStateFromRemoteService(CancellationToken.None);

            // Then
            capturedState.Should().NotBeNull();
            capturedState!.Summary.Should().Be(string.Empty);
            capturedState.Status.Should().Be("Unknown");
            capturedState.IssueType.Should().Be("Unknown");
            capturedState.ProjectKey.Should().Be("Unknown");
            capturedState.Created.Should().BeNull();
            capturedState.AssigneeDisplayName.Should().BeNull();
            capturedState.BrowseUrl.Should().Be("https://test.atlassian.net/browse/PROJ-789");
        }

        [Fact]
        public async Task GivenMissingApiUrl_WhenSyncStateFromRemoteService_ThenUsesHashAsUrl()
        {
            // Given
            _jiraSettingsMock.Setup(s => s.GetSettings())
                .Returns(JiraSettings.CreateDefaultSettings());
            
            var sut = new JiraCurrentStateService(
                _restApiClientMock.Object,
                _issueRepositoryMock.Object,
                _jiraSettingsMock.Object,
                _clockMock.Object,
                _loggerMock.Object);

            var currentUser = new JiraUser("testuser", "Test User", "account123");
            var currentTime = new DateTime(2026, 1, 18, 12, 0, 0, DateTimeKind.Utc);

            var issueResponses = new List<JiraIssueResponse>
            {
                new JiraIssueResponse(
                    "PROJ-1",
                    "10001",
                    new JiraIssueFieldsResponse(
                        "Test",
                        new DateTimeOffset(2026, 1, 18, 10, 0, 0, TimeSpan.Zero),
                        null,
                        new JiraStatusResponse("Open"),
                        null,
                        null,
                        new JiraIssueTypeResponse("Bug", false),
                        new JiraProjectResponse("PROJ", "Project"),
                        null),
                    null)
            };

            _clockMock.Setup(c => c.Now).Returns(currentTime);
            _restApiClientMock.Setup(c => c.GetCurrentUser()).ReturnsAsync(currentUser);
            _restApiClientMock.Setup(c => c.GetUserIssues(It.IsAny<JiraUser>(), It.IsAny<DateTime>()))
                .ReturnsAsync(issueResponses);

            JiraIssueState? capturedState = null;
            _issueRepositoryMock.Setup(r => r.UpdateCurrentStateAsync(
                It.IsAny<IEnumerable<JiraIssueState>>(),
                It.IsAny<CancellationToken>()))
                .Callback<IEnumerable<JiraIssueState>, CancellationToken>((states, _) =>
                {
                    capturedState = states.First();
                })
                .Returns(Task.CompletedTask);

            // When
            await sut.SyncStateFromRemoteService(CancellationToken.None);

            // Then
            capturedState.Should().NotBeNull();
            capturedState!.BrowseUrl.Should().Be("#");
        }

        [Fact]
        public async Task GivenInvalidApiUrl_WhenSyncStateFromRemoteService_ThenUsesHashAsUrl()
        {
            // Given
            _jiraSettingsMock.Setup(s => s.GetSettings())
                .Returns(new JiraSettings 
                { 
                    ApiUrl = "not-a-valid-url",
                    ApiKey = string.Empty
                });
            
            var sut = new JiraCurrentStateService(
                _restApiClientMock.Object,
                _issueRepositoryMock.Object,
                _jiraSettingsMock.Object,
                _clockMock.Object,
                _loggerMock.Object);

            var currentUser = new JiraUser("testuser", "Test User", "account123");
            var currentTime = new DateTime(2026, 1, 18, 12, 0, 0, DateTimeKind.Utc);

            var issueResponses = new List<JiraIssueResponse>
            {
                new JiraIssueResponse(
                    "PROJ-1",
                    "10001",
                    new JiraIssueFieldsResponse(
                        "Test",
                        new DateTimeOffset(2026, 1, 18, 10, 0, 0, TimeSpan.Zero),
                        null,
                        new JiraStatusResponse("Open"),
                        null,
                        null,
                        new JiraIssueTypeResponse("Bug", false),
                        new JiraProjectResponse("PROJ", "Project"),
                        null),
                    null)
            };

            _clockMock.Setup(c => c.Now).Returns(currentTime);
            _restApiClientMock.Setup(c => c.GetCurrentUser()).ReturnsAsync(currentUser);
            _restApiClientMock.Setup(c => c.GetUserIssues(It.IsAny<JiraUser>(), It.IsAny<DateTime>()))
                .ReturnsAsync(issueResponses);

            JiraIssueState? capturedState = null;
            _issueRepositoryMock.Setup(r => r.UpdateCurrentStateAsync(
                It.IsAny<IEnumerable<JiraIssueState>>(),
                It.IsAny<CancellationToken>()))
                .Callback<IEnumerable<JiraIssueState>, CancellationToken>((states, _) =>
                {
                    capturedState = states.First();
                })
                .Returns(Task.CompletedTask);

            // When
            await sut.SyncStateFromRemoteService(CancellationToken.None);

            // Then
            capturedState.Should().NotBeNull();
            capturedState!.BrowseUrl.Should().Be("#");
        }
    }
}
