using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using TrackYourDay.Core.ApplicationTrackers.GitLab;
using TrackYourDay.Core.ApplicationTrackers.GitLab.PublicEvents;
using TrackYourDay.Core.Persistence;
using TrackYourDay.Core.Persistence.Specifications;

namespace TrackYourDay.Tests.ApplicationTrackers.GitLab
{
    [Trait("Category", "Unit")]
    public class GitLabTrackerTests
    {
        private readonly Mock<IGitLabActivityService> _activityServiceMock;
        private readonly Mock<IHistoricalDataRepository<GitLabActivity>> _repositoryMock;
        private readonly Mock<IGitLabSettingsService> _settingsServiceMock;
        private readonly Mock<IPublisher> _publisherMock;
        private readonly Mock<ILogger<GitLabTracker>> _loggerMock;
        private readonly GitLabTracker _tracker;

        public GitLabTrackerTests()
        {
            _activityServiceMock = new Mock<IGitLabActivityService>();
            _repositoryMock = new Mock<IHistoricalDataRepository<GitLabActivity>>();
            _settingsServiceMock = new Mock<IGitLabSettingsService>();
            _publisherMock = new Mock<IPublisher>();
            _loggerMock = new Mock<ILogger<GitLabTracker>>();

            _tracker = new GitLabTracker(
                _activityServiceMock.Object,
                _repositoryMock.Object,
                _settingsServiceMock.Object,
                _publisherMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task GivenNoActivitiesExist_WhenRecognizingActivity_ThenNoEventsArePublished()
        {
            // Given
            var syncTime = DateTime.UtcNow;
            var settings = new GitLabSettings { ApiUrl = "https://test.com", ApiKey = "test-key", LastSyncTimestamp = syncTime.AddDays(-1) };
            
            _settingsServiceMock.Setup(s => s.GetSettings()).Returns(settings);
            _activityServiceMock
                .Setup(s => s.GetActivitiesUpdatedAfter(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<GitLabActivity>());

            // When
            var result = await _tracker.RecognizeActivitiesAsync();

            // Then
            result.Should().Be(0);
            _publisherMock.Verify(
                p => p.Publish(It.IsAny<GitLabActivityDiscoveredEvent>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GivenNewActivitiesExist_WhenRecognizingActivity_ThenEventsArePublishedForEachNewActivity()
        {
            // Given
            var watermark = DateTime.UtcNow.AddDays(-1);
            var settings = new GitLabSettings { ApiUrl = "https://test.com", ApiKey = "test-key", LastSyncTimestamp = watermark };
            var activity1 = new GitLabActivity { UpstreamId = "gitlab-1", OccuranceDate = watermark.AddHours(1), Description = "Opened Issue" };
            var activity2 = new GitLabActivity { UpstreamId = "gitlab-2", OccuranceDate = watermark.AddHours(2), Description = "Closed Issue" };
            
            _settingsServiceMock.Setup(s => s.GetSettings()).Returns(settings);
            _activityServiceMock
                .Setup(s => s.GetActivitiesUpdatedAfter(watermark, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<GitLabActivity> { activity1, activity2 });
            _repositoryMock
                .Setup(r => r.TryAppendAsync(It.IsAny<GitLabActivity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // When
            var result = await _tracker.RecognizeActivitiesAsync();

            // Then
            result.Should().Be(2);
            _publisherMock.Verify(
                p => p.Publish(It.Is<GitLabActivityDiscoveredEvent>(e => e.Activity == activity1), It.IsAny<CancellationToken>()),
                Times.Once);
            _publisherMock.Verify(
                p => p.Publish(It.Is<GitLabActivityDiscoveredEvent>(e => e.Activity == activity2), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GivenActivitiesAlreadyInRepository_WhenRecognizingActivity_ThenNoEventsArePublished()
        {
            // Given
            var watermark = DateTime.UtcNow.AddDays(-1);
            var settings = new GitLabSettings { ApiUrl = "https://test.com", ApiKey = "test-key", LastSyncTimestamp = watermark };
            var activity = new GitLabActivity { UpstreamId = "gitlab-3", OccuranceDate = watermark.AddHours(1), Description = "Duplicate" };

            _settingsServiceMock.Setup(s => s.GetSettings()).Returns(settings);
            _activityServiceMock
                .Setup(s => s.GetActivitiesUpdatedAfter(watermark, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<GitLabActivity> { activity });
            _repositoryMock
                .Setup(r => r.TryAppendAsync(activity, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false); // Already exists

            // When
            var result = await _tracker.RecognizeActivitiesAsync();

            // Then
            result.Should().Be(0);
            _publisherMock.Verify(
                p => p.Publish(It.IsAny<GitLabActivityDiscoveredEvent>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GivenMixOfNewAndExistingActivities_WhenRecognizingActivity_ThenOnlyNewActivitiesArePublished()
        {
            // Given
            var watermark = DateTime.UtcNow.AddDays(-1);
            var settings = new GitLabSettings { ApiUrl = "https://test.com", ApiKey = "test-key", LastSyncTimestamp = watermark };
            var newActivity = new GitLabActivity { UpstreamId = "gitlab-new", OccuranceDate = watermark.AddHours(1), Description = "New" };
            var existingActivity = new GitLabActivity { UpstreamId = "gitlab-existing", OccuranceDate = watermark.AddHours(2), Description = "Existing" };

            _settingsServiceMock.Setup(s => s.GetSettings()).Returns(settings);
            _activityServiceMock
                .Setup(s => s.GetActivitiesUpdatedAfter(watermark, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<GitLabActivity> { newActivity, existingActivity });
            _repositoryMock
                .Setup(r => r.TryAppendAsync(newActivity, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _repositoryMock
                .Setup(r => r.TryAppendAsync(existingActivity, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // When
            var result = await _tracker.RecognizeActivitiesAsync();

            // Then
            result.Should().Be(1);
            _publisherMock.Verify(
                p => p.Publish(It.Is<GitLabActivityDiscoveredEvent>(e => e.Activity == newActivity), It.IsAny<CancellationToken>()),
                Times.Once);
            _publisherMock.Verify(
                p => p.Publish(It.Is<GitLabActivityDiscoveredEvent>(e => e.Activity == existingActivity), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task WhenRecognizingActivities_ThenWatermarkIsUpdatedToCurrentTime()
        {
            // Given
            var watermark = DateTime.UtcNow.AddDays(-1);
            var settings = new GitLabSettings { ApiUrl = "https://test.com", ApiKey = "test-key", LastSyncTimestamp = watermark };
            
            _settingsServiceMock.Setup(s => s.GetSettings()).Returns(settings);
            _activityServiceMock
                .Setup(s => s.GetActivitiesUpdatedAfter(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<GitLabActivity>());

            // When
            await _tracker.RecognizeActivitiesAsync();

            // Then
            _settingsServiceMock.Verify(s => s.UpdateLastSyncTimestamp(It.IsAny<DateTime>()), Times.Once);
            _settingsServiceMock.Verify(s => s.PersistSettings(), Times.Once);
        }

        [Fact]
        public async Task GivenNoLastSyncTimestamp_WhenRecognizingActivities_ThenUses7DaysAgoAsDefault()
        {
            // Given
            var settings = new GitLabSettings { ApiUrl = "https://test.com", ApiKey = "test-key", LastSyncTimestamp = null };
            var capturedWatermark = DateTime.MinValue;
            
            _settingsServiceMock.Setup(s => s.GetSettings()).Returns(settings);
            _activityServiceMock
                .Setup(s => s.GetActivitiesUpdatedAfter(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<GitLabActivity>())
                .Callback<DateTime, CancellationToken>((date, ct) => capturedWatermark = date);

            // When
            await _tracker.RecognizeActivitiesAsync();

            // Then - Should have fetched from approximately 7 days ago (LastSyncTimestamp null defaults to UtcNow - 7 days)
            var expectedDate = DateTime.UtcNow.AddDays(-7);
            capturedWatermark.Should().BeCloseTo(expectedDate, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task GivenDeduplicationMismatch_WhenActivityNewByGuidButOldByTimestamp_ThenLogsWarning()
        {
            // Given - Activity is NEW by GUID but BEFORE watermark (timestamp says old)
            var watermark = DateTime.UtcNow.AddDays(-1);
            var settings = new GitLabSettings { ApiUrl = "https://test.com", ApiKey = "test-key", LastSyncTimestamp = watermark };
            var activity = new GitLabActivity 
            { 
                UpstreamId = "gitlab-mismatch", 
                OccuranceDate = watermark.AddHours(-1), // Before watermark
                Description = "Mismatch" 
            };

            _settingsServiceMock.Setup(s => s.GetSettings()).Returns(settings);
            _activityServiceMock
                .Setup(s => s.GetActivitiesUpdatedAfter(watermark, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<GitLabActivity> { activity });
            _repositoryMock
                .Setup(r => r.TryAppendAsync(activity, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true); // NEW by GUID

            // When
            await _tracker.RecognizeActivitiesAsync();

            // Then - Warning should be logged
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Deduplication mismatch")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task WhenGettingActivities_ThenDelegatesToRepository()
        {
            // Given
            var fromDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-7));
            var toDate = DateOnly.FromDateTime(DateTime.Today);
            var expectedActivities = new List<GitLabActivity>
            {
                new() { UpstreamId = "gitlab-1", OccuranceDate = DateTime.Today.AddDays(-3), Description = "Activity 1" },
                new() { UpstreamId = "gitlab-2", OccuranceDate = DateTime.Today.AddDays(-1), Description = "Activity 2" }
            };

            _repositoryMock
                .Setup(r => r.FindAsync(It.IsAny<ISpecification<GitLabActivity>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedActivities);

            // When
            var result = await _tracker.GetActivitiesAsync(fromDate, toDate);

            // Then
            result.Should().BeEquivalentTo(expectedActivities);
            _repositoryMock.Verify(
                r => r.FindAsync(It.Is<DateRangeSpecification<GitLabActivity>>(s => true), It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
