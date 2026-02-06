using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using TrackYourDay.Core.ApplicationTrackers.Breaks;
using TrackYourDay.Core.ApplicationTrackers.Breaks.Events;
using TrackYourDay.Core.Persistence;
using TrackYourDay.Core.Persistence.EventHandlers;

namespace TrackYourDay.Tests.Persistence
{
    public class PersistRevokedBreakHandlerTests
    {
        [Fact]
        public async Task GivenValidRevokedBreakEvent_WhenHandling_ThenUpdatesBreakWithRevokedAtTimestamp()
        {
            // Given
            var breakGuid = Guid.NewGuid();
            var breakStartedAt = new DateTime(2026, 2, 6, 10, 0, 0);
            var breakEndedAt = new DateTime(2026, 2, 6, 10, 15, 0);
            var revokedAt = new DateTime(2026, 2, 6, 10, 20, 0);

            var endedBreak = new EndedBreak(breakGuid, breakStartedAt, breakEndedAt, "Coffee break");
            var revokedBreak = new RevokedBreak(endedBreak, revokedAt);
            var breakRevokedEvent = new BreakRevokedEvent(revokedBreak);

            var mockRepository = new Mock<IHistoricalDataRepository<EndedBreak>>();
            var mockLogger = new Mock<ILogger<PersistRevokedBreakHandler>>();
            var handler = new PersistRevokedBreakHandler(mockRepository.Object, mockLogger.Object);

            // When
            await handler.Handle(breakRevokedEvent, CancellationToken.None);

            // Then
            mockRepository.Verify(r => r.Update(It.Is<EndedBreak>(b =>
                b.Guid == breakGuid &&
                b.RevokedAt == revokedAt &&
                b.BreakStartedAt == breakStartedAt &&
                b.BreakEndedAt == breakEndedAt &&
                b.BreakDescription == "Coffee break"
            )), Times.Once);
        }

        [Fact]
        public async Task GivenRepositoryThrowsException_WhenHandling_ThenLogsErrorAndRethrows()
        {
            // Given
            var endedBreak = new EndedBreak(Guid.NewGuid(), DateTime.Now, DateTime.Now.AddMinutes(15), "Test break");
            var revokedBreak = new RevokedBreak(endedBreak, DateTime.Now);
            var breakRevokedEvent = new BreakRevokedEvent(revokedBreak);

            var mockRepository = new Mock<IHistoricalDataRepository<EndedBreak>>();
            mockRepository.Setup(r => r.Update(It.IsAny<EndedBreak>()))
                .Throws(new InvalidOperationException("Database error"));

            var mockLogger = new Mock<ILogger<PersistRevokedBreakHandler>>();
            var handler = new PersistRevokedBreakHandler(mockRepository.Object, mockLogger.Object);

            // When
            Func<Task> act = async () => await handler.Handle(breakRevokedEvent, CancellationToken.None);

            // Then
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Database error");

            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}
