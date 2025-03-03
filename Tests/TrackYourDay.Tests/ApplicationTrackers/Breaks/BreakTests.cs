using FluentAssertions;
using TrackYourDay.Core.ApplicationTrackers.Breaks;

namespace TrackYourDay.Tests.ApplicationTrackers.Breaks
{
    public class BreakTests
    {
        [Fact]
        public void BreakCannotEndBeforeItStarted()
        {
            // Arrange
            var breakStartedOn = DateTime.Now;
            var breakEndedOn = breakStartedOn.AddMinutes(-1);
            var startedBreak = new StartedBreak(breakStartedOn, string.Empty);

            // Act and Assert
            Assert.Throws<ArgumentException>(() => startedBreak.EndBreak(breakEndedOn));
        }

        [Fact]
        public void BreakCannotEndOnOtherDay()
        {
            // Arrange
            var breakStartedOn = DateTime.Now;
            var breakEndedOn = breakStartedOn.AddDays(1);
            var startedBreak = new StartedBreak(breakStartedOn, string.Empty);

            // Act and Assert
            Assert.Throws<ArgumentException>(() => startedBreak.EndBreak(breakEndedOn));
        }

        [Fact]
        public void BreakDurationIsCorrect()
        {
            // Arrange
            var breakStartedOn = DateTime.Now;
            var breakEndedOn = breakStartedOn.AddMinutes(1);
            var breakEnded = new EndedBreak(Guid.Empty, breakStartedOn, breakEndedOn, string.Empty);

            // Act and Assert
            breakEnded.BreakDuration.Should().Be(TimeSpan.FromMinutes(1));
        }

        [Fact]
        public void WhenEndedBreakIsRevoked_ThenItBecomesRevokedBreak()
        {
            // Arrange
            var endedBreak = new EndedBreak(Guid.Empty, DateTime.Now, DateTime.Now.AddMinutes(5), "test");

            // Act
            var revokedBreak = endedBreak.Revoke(DateTime.Now);

            // Assert
            revokedBreak.Should().BeOfType<RevokedBreak>();
        }
    }
}
