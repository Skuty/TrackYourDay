using FluentAssertions;
using TrackYourDay.Core.ApplicationTrackers.Breaks;

namespace TrackYourDay.Tests.ApplicationTrackers.Breaks
{
    public class EndedBreakRevokedAtTests
    {
        [Fact]
        public void GivenEndedBreak_WhenMarkingAsRevoked_ThenCreatesNewInstanceWithRevokedAtTimestamp()
        {
            // Given
            var breakGuid = Guid.NewGuid();
            var breakStartedAt = new DateTime(2026, 2, 6, 10, 0, 0);
            var breakEndedAt = new DateTime(2026, 2, 6, 10, 15, 0);
            var revokedAt = new DateTime(2026, 2, 6, 10, 20, 0);
            
            var endedBreak = new EndedBreak(breakGuid, breakStartedAt, breakEndedAt, "Coffee break");

            // When
            var revokedEndedBreak = endedBreak.MarkAsRevoked(revokedAt);

            // Then
            revokedEndedBreak.Should().NotBeSameAs(endedBreak);
            revokedEndedBreak.Guid.Should().Be(breakGuid);
            revokedEndedBreak.BreakStartedAt.Should().Be(breakStartedAt);
            revokedEndedBreak.BreakEndedAt.Should().Be(breakEndedAt);
            revokedEndedBreak.BreakDescription.Should().Be("Coffee break");
            revokedEndedBreak.RevokedAt.Should().Be(revokedAt);
        }

        [Fact]
        public void GivenEndedBreak_WhenCreated_ThenRevokedAtIsNull()
        {
            // Given & When
            var endedBreak = new EndedBreak(
                Guid.NewGuid(), 
                new DateTime(2026, 2, 6, 10, 0, 0),
                new DateTime(2026, 2, 6, 10, 15, 0),
                "Test break");

            // Then
            endedBreak.RevokedAt.Should().BeNull();
        }

        [Fact]
        public void GivenEndedBreak_WhenInitializedWithRevokedAt_ThenRevokedAtIsSet()
        {
            // Given
            var revokedAt = new DateTime(2026, 2, 6, 10, 20, 0);
            
            // When
            var endedBreak = new EndedBreak(
                Guid.NewGuid(), 
                new DateTime(2026, 2, 6, 10, 0, 0),
                new DateTime(2026, 2, 6, 10, 15, 0),
                "Test break")
            {
                RevokedAt = revokedAt
            };

            // Then
            endedBreak.RevokedAt.Should().Be(revokedAt);
        }
    }
}
