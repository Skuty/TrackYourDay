using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TrackYourDay.Core.ApplicationTrackers.GitLab;

namespace TrackYourDay.Tests.ApplicationTrackers.GitLab
{
    [Trait("Category", "Integration")]
    public class GitLabRestApiClientTests
    {
        private readonly GitLabRestApiClient client;
        private readonly DateOnly startingDate = new DateOnly(2024, 03, 16);
        private readonly long projectId = 24674429;
        private readonly string refName = "master";

        public GitLabRestApiClientTests()
        {
            var mockLogger = new Mock<ILogger<GitLabRestApiClient>>();
            this.client = new GitLabRestApiClient("https://gitlab.com", "", mockLogger.Object);
        }

        [Fact]
        public async Task WhenGettingUserForSuppliedCredentials_ThenAuthenticatedUserDetailsAreReturned()
        {
            // When
            var user = await this.client.GetCurrentUser();

            // Then
            Assert.NotNull(user);
        }

        [Fact]
        public async Task GivenUserIsAuthenticated_WhenGettingEventsOfUser_ThenListOfEventsIsReturned()
        {
            // Given
            var user = await this.client.GetCurrentUser();

            // When
            var events = await this.client.GetUserEvents(new GitLabUserId(user.Id), this.startingDate);

            // Then
            Assert.NotNull(events);
            Assert.IsAssignableFrom<DateTime>(events.First().CreatedAt.DateTime);
        }

        [Fact]
        public async Task GivenUserIsAuthenticated_WhenGettingEventsContaningNote_ThenNoteDetailsAreSerializedProperly()
        {
            // Given
            var user = await this.client.GetCurrentUser();

            // When
            var events = await this.client.GetUserEvents(new GitLabUserId(user.Id), this.startingDate);
            var eventWithNote = events.FirstOrDefault(e => e.TargetType == "Note");

            // Then
            Assert.NotNull(eventWithNote);
        }

        [Fact]
        public async Task GivenUserIsAuthenticated_WhenGettingEventsContaningPush_ThenPushDetailsAreSerializedProperly()
        {
            // Given
            var user = await this.client.GetCurrentUser();

            // When
            var events = await this.client.GetUserEvents(new GitLabUserId(user.Id), this.startingDate);
            var eventWithPush = events.FirstOrDefault(e => e.Action.Contains("pushed"));

            // Then
            Assert.NotNull(eventWithPush.PushData);
        }

        [Fact]
        public async Task GivenUserIsAuthenticated_WhenGettingGitLabProject_ThenProjectIsSerializedProperly()
        {
            // Given
            var user = await this.client.GetCurrentUser();

            // When
            var project = await this.client.GetProject(new GitLabProjectId(this.projectId));

            // Then
            Assert.NotNull(project);
            Assert.NotEmpty(project.Name);
        }

        [Fact]
        public async Task GivenUserIsAuthenticated_WhenGettingGitLabCommits_ThenCommitsAreSerializedProperly()
        {
            // Given
            var user = await this.client.GetCurrentUser();

            // When
            var commits = await this.client.GetCommits(new GitLabProjectId(this.projectId), new GitLabRefName(this.refName), this.startingDate);
            
            // Then
            Assert.NotNull(commits);
            commits.Count.Should().Be(6);
            commits.ForEach(c => c.Message.Should().NotBeNullOrEmpty());
        }
    }
}