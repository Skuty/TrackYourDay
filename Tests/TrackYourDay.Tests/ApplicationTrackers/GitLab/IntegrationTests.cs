using TrackYourDay.Core.ApplicationTrackers.GitLab;

namespace TrackYourDay.Tests.ApplicationTrackers.GitLab
{
    [Trait("Category", "Integration")]
    public class IntegrationTests
    {
        private readonly GitLabRestApiClient client;

        public IntegrationTests()
        {
            this.client = new GitLabRestApiClient("https://gitlab.com", "secret");
        }

        [Fact]
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
            var events = this.client.GetUserEvents(user.Id, new DateOnly(2021, 02, 21));

            // Then
            Assert.NotNull(events);
        }

        [Fact]
        public void GivenUserIsAuthenticated_WhenGettingEventsContaningNote_ThenNoteDetailsAreSerializedProperly()
        {
            // Given
            var user = this.client.GetCurrentUser();

            // When
            var events = this.client.GetUserEvents(user.Id, new DateOnly(2021, 02, 21));
            var eventWithNote = events.FirstOrDefault(e => e.TargetType == "Note");

            // Then
            Assert.NotNull(eventWithNote.Note);
        }

        [Fact]
        public void GivenUserIsAuthenticated_WhenGettingEventsContaningPush_ThenPushDetailsAreSerializedProperly()
        {
            // Given
            var user = this.client.GetCurrentUser();

            // When
            var events = this.client.GetUserEvents(user.Id, new DateOnly(2021, 02, 21));
            var eventWithPush = events.FirstOrDefault(e => e.TargetType == "Pushed");

            // Then
            Assert.NotNull(eventWithPush.PushData);
        }
    }
}
