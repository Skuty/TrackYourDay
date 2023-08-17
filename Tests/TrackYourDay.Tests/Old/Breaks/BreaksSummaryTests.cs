using FluentAssertions;
using TrackYourDay.Core.Breaks;
using TrackYourDay.Core.Old.Breaks;

namespace TrackYourDay.Tests.Old.Breaks
{
    public class BreaksSummaryTests
    {
        [Fact]
        public void GivenThereIsZeroBreaks_WhenGettingTimeOfAllBreaks_ThenTimeEqualToZeroIsReturned()
        {
            // Arrange
            var breaks = new List<EndedBreak>();

            // Act
            var sut = new BreaksSummary(breaks);

            // Assert
            sut.GetTimeOfAllBreaks().Should().Be(TimeSpan.Zero);
        }

        [Fact]
        public void GivenThereIsAtLeastOneBreak_WhenGettingTimeOfAllBreaks_ThenSummedTimeOfAllBreakDurationsIsReturned()
        {
            // Arrange
            var breaks = new List<EndedBreak>
            {
                new EndedBreak(DateTime.Parse("2000-01-01 12:00:00"), DateTime.Parse("2000-01-01 12:01:00"), string.Empty),
                new EndedBreak(DateTime.Parse("2000-01-01 12:10:00"), DateTime.Parse("2000-01-01 12:15:00"), string.Empty),
                new EndedBreak(DateTime.Parse("2000-01-01 13:00:00"), DateTime.Parse("2000-01-01 14:15:00"), string.Empty),
            };

            // Act
            var sut = new BreaksSummary(breaks);

            // Assert
            sut.GetTimeOfAllBreaks().Should().Be(TimeSpan.FromMinutes(81));
        }

        [Fact]
        public void WhenGettingAmountOfAllBreaks_ThenAmountOfAllBreaksIsReturned()
        {
            // Arrange
            var breaks = new List<EndedBreak>
            {
                new EndedBreak(DateTime.Now, DateTime.Now.AddMinutes(1), string.Empty),
                new EndedBreak(DateTime.Now, DateTime.Now.AddMinutes(1), string.Empty),
                new EndedBreak(DateTime.Now, DateTime.Now.AddMinutes(1), string.Empty),
            };

            // Act
            var sut = new BreaksSummary(breaks);

            // Assert
            sut.GetCountOfAllBreaks().Should().Be(3);
        }
    }
}
