using FluentAssertions;
using TrackYourDay.Core.Breaks;

namespace TrackYourDay.Tests.Old.Breaks
{
    public class BreakTests
    {
        [Fact]
        public void BreakCannotEndBeforeItStarted()
        {
            // Arrange
            var breakStartedOn = DateTime.Now;
            var breakEndedOn = breakStartedOn.AddMinutes(-1);
            var startedBreak = new StartedBreak(breakStartedOn);

            // Act and Assert
            Assert.Throws<ArgumentException>(() => startedBreak.EndBreak(breakEndedOn));
        }

        [Fact]
        public void BreakCannotEndOnOtherDay()
        {
            // Arrange
            var breakStartedOn = DateTime.Now;
            var breakEndedOn = breakStartedOn.AddDays(1);
            var startedBreak = new StartedBreak(breakStartedOn);

            // Act and Assert
            Assert.Throws<ArgumentException>(() => startedBreak.EndBreak(breakEndedOn));
        }

        [Fact]
        public void BreakDurationIsCorrect()
        {
            // Arrange
            var breakStartedOn = DateTime.Now;
            var breakEndedOn = breakStartedOn.AddMinutes(1);
            var breakEnded = new EndedBreak(breakStartedOn, breakEndedOn);

            // Act and Assert
            breakEnded.BreakDuration.Should().Be(TimeSpan.FromMinutes(1));
        }
    }
}
