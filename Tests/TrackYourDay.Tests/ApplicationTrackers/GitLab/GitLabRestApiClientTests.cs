using TrackYourDay.Core.ApplicationTrackers.GitLab;

namespace TrackYourDay.Tests.ApplicationTrackers.GitLab
{
    [Trait("Category", "Integration")]
    public class GitLabRestApiClientTests
    {
        private readonly GitLabRestApiClient client;
        private readonly DateOnly eventsStartingDate = new DateOnly(2024, 03, 16);

        public GitLabRestApiClientTests()
        {
            this.client = new GitLabRestApiClient("https://gitlab.com", "secret");
        }

        //[Fact]
        public void WhenGettingUserForSuppliedCredentials_ThenAuthenticatedUserDetailsAreReturned()
        {
            // When
            var user = this.client.GetCurrentUser();

            // Then
            Assert.NotNull(user);
        }

        [Fact]
        public void GivenUserIsAuthenticated_WhenGettingEventsOfUser_ThenListOfEventsIsReturned()
        {
            // Given
            var user = this.client.GetCurrentUser();

            // When
            var events = this.client.GetUserEvents(new GitLabUserId(user.Id), this.eventsStartingDate);

            // Then
            Assert.NotNull(events);
            Assert.IsAssignableFrom<DateTime>(events.First().CreatedAt.DateTime);
        }

        //[Fact]
        public void GivenUserIsAuthenticated_WhenGettingEventsContaningNote_ThenNoteDetailsAreSerializedProperly()
        {
            // Given
            var user = this.client.GetCurrentUser();

            // When
            var events = this.client.GetUserEvents(new GitLabUserId(user.Id), this.eventsStartingDate);
            var eventWithNote = events.FirstOrDefault(e => e.TargetType == "Note");

            // Then
            Assert.NotNull(eventWithNote.Note);
        }

        //[Fact]
        public void GivenUserIsAuthenticated_WhenGettingEventsContaningPush_ThenPushDetailsAreSerializedProperly()
        {
            // Given
            var user = this.client.GetCurrentUser();

            // When
            var events = this.client.GetUserEvents(new GitLabUserId(user.Id), this.eventsStartingDate);
            var eventWithPush = events.FirstOrDefault(e => e.Action.Contains("pushed"));

            // Then
            Assert.NotNull(eventWithPush.PushData);
        }
    }
}
