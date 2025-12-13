using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using TrackYourDay.Core.ApplicationTrackers.GitLab;
using TrackYourDay.Core.ApplicationTrackers.GitLab.PublicEvents;
using TrackYourDay.Core.Persistence;
using TrackYourDay.Core.Persistence.EventHandlers;

namespace TrackYourDay.Tests.Persistence.EventHandlers
{
    [Trait("Category", "Unit")]
    public class PersistGitLabActivityHandlerTests
    {
        private Mock<IHistoricalDataRepository<DiscoveredGitLabActivity>> repositoryMock;
        private Mock<ILogger<PersistGitLabActivityHandler>> loggerMock;
        private PersistGitLabActivityHandler handler;

        public PersistGitLabActivityHandlerTests()
        {
            this.repositoryMock = new Mock<IHistoricalDataRepository<DiscoveredGitLabActivity>>();
            this.loggerMock = new Mock<ILogger<PersistGitLabActivityHandler>>();
            this.handler = new PersistGitLabActivityHandler(
                this.repositoryMock.Object,
                this.loggerMock.Object);
        }

        [Fact]
        public async Task GivenGitLabActivityDiscoveredEvent_WhenHandled_ThenActivityIsPersisted()
        {
            // Given
            var guid = Guid.NewGuid();
            var occuranceDate = new DateTime(2025, 03, 16, 10, 0, 0);
            var description = "Opened Issue: Test issue";
            var gitLabActivity = new GitLabActivity(occuranceDate, description);
            var notification = new GitLabActivityDiscoveredEvent(guid, gitLabActivity);

            // When
            await this.handler.Handle(notification, CancellationToken.None);

            // Then
            this.repositoryMock.Verify(
                r => r.Save(It.Is<DiscoveredGitLabActivity>(
                    a => a.Guid == guid 
                      && a.OccuranceDate == occuranceDate 
                      && a.Description == description)),
                Times.Once);
        }

        [Fact]
        public async Task GivenGitLabActivityDiscoveredEvent_WhenHandled_ThenSuccessIsLogged()
        {
            // Given
            var guid = Guid.NewGuid();
            var occuranceDate = new DateTime(2025, 03, 16, 10, 0, 0);
            var description = "Opened Issue: Test issue";
            var gitLabActivity = new GitLabActivity(occuranceDate, description);
            var notification = new GitLabActivityDiscoveredEvent(guid, gitLabActivity);

            // When
            await this.handler.Handle(notification, CancellationToken.None);

            // Then
            this.loggerMock.Verify(
                l => l.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Persisted GitLab activity")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GivenRepositoryThrowsException_WhenHandled_ThenExceptionIsPropagated()
        {
            // Given
            var guid = Guid.NewGuid();
            var occuranceDate = new DateTime(2025, 03, 16, 10, 0, 0);
            var description = "Opened Issue: Test issue";
            var gitLabActivity = new GitLabActivity(occuranceDate, description);
            var notification = new GitLabActivityDiscoveredEvent(guid, gitLabActivity);

            this.repositoryMock
                .Setup(r => r.Save(It.IsAny<DiscoveredGitLabActivity>()))
                .Throws(new InvalidOperationException("Database error"));

            // When
            Func<Task> act = async () => await this.handler.Handle(notification, CancellationToken.None);

            // Then
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Database error");
        }

        [Fact]
        public async Task GivenRepositoryThrowsException_WhenHandled_ThenErrorIsLogged()
        {
            // Given
            var guid = Guid.NewGuid();
            var occuranceDate = new DateTime(2025, 03, 16, 10, 0, 0);
            var description = "Opened Issue: Test issue";
            var gitLabActivity = new GitLabActivity(occuranceDate, description);
            var notification = new GitLabActivityDiscoveredEvent(guid, gitLabActivity);

            this.repositoryMock
                .Setup(r => r.Save(It.IsAny<DiscoveredGitLabActivity>()))
                .Throws(new InvalidOperationException("Database error"));

            // When
            try
            {
                await this.handler.Handle(notification, CancellationToken.None);
            }
            catch
            {
                // Expected exception
            }

            // Then
            this.loggerMock.Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to persist GitLab activity")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}
