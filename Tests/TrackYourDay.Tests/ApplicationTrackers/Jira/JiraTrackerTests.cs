using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TrackYourDay.Core.ApplicationTrackers.Jira;
using TrackYourDay.Core.ApplicationTrackers.Persistence;

namespace TrackYourDay.Tests.ApplicationTrackers.Jira
{
    [Trait("Category", "Unit")]
    public class JiraTrackerTests
    {
        private readonly Mock<IJiraActivityService> _activityServiceMock;
        private readonly Mock<IJiraActivityRepository> _repositoryMock;
        private readonly Mock<IJiraSettingsService> _settingsServiceMock;
        private readonly Mock<ILogger<JiraTracker>> _loggerMock;
        private readonly JiraTracker _tracker;

        public JiraTrackerTests()
        {
            _activityServiceMock = new Mock<IJiraActivityService>();
            _repositoryMock = new Mock<IJiraActivityRepository>();
            _settingsServiceMock = new Mock<IJiraSettingsService>();
            _loggerMock = new Mock<ILogger<JiraTracker>>();

            _tracker = new JiraTracker(
                _activityServiceMock.Object,
                _repositoryMock.Object,
                _settingsServiceMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task GivenNoActivitiesExist_WhenRecognizingActivities_ThenReturnsZero()
        {
            // Given
            var syncTime = DateTime.UtcNow;
            var settings = new JiraSettings { ApiUrl = "https://test.com", ApiKey = "test-key", LastSyncTimestamp = syncTime.AddDays(-1) };
            
            _settingsServiceMock.Setup(s => s.GetSettings()).Returns(settings);
            _activityServiceMock
                .Setup(s => s.GetActivitiesUpdatedAfter(It.IsAny<DateTime>()))
                .ReturnsAsync(new List<JiraActivity>());

            // When
            var result = await _tracker.RecognizeActivitiesAsync();

            // Then
            result.Should().Be(0);
        }

        [Fact]
        public async Task GivenNewActivitiesExist_WhenRecognizingActivities_ThenReturnsNewCount()
        {
            // Given
            var watermark = DateTime.UtcNow.AddDays(-1);
            var settings = new JiraSettings { ApiUrl = "https://test.com", ApiKey = "test-key", LastSyncTimestamp = watermark };
            var activity1 = new JiraActivity { UpstreamId = "jira-1", OccurrenceDate = watermark.AddHours(1), Description = "Issue created" };
            var activity2 = new JiraActivity { UpstreamId = "jira-2", OccurrenceDate = watermark.AddHours(2), Description = "Worklog added" };
            
            _settingsServiceMock.Setup(s => s.GetSettings()).Returns(settings);
            _activityServiceMock
                .Setup(s => s.GetActivitiesUpdatedAfter(watermark))
                .ReturnsAsync(new List<JiraActivity> { activity1, activity2 });
            _repositoryMock
                .Setup(r => r.TryAppendAsync(It.IsAny<JiraActivity>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // When
            var result = await _tracker.RecognizeActivitiesAsync();

            // Then
            result.Should().Be(2);
        }

        [Fact]
        public async Task GivenActivitiesAlreadyInRepository_WhenRecognizingActivities_ThenReturnsZero()
        {
            // Given
            var watermark = DateTime.UtcNow.AddDays(-1);
            var settings = new JiraSettings { ApiUrl = "https://test.com", ApiKey = "test-key", LastSyncTimestamp = watermark };
            var activity = new JiraActivity { UpstreamId = "jira-duplicate", OccurrenceDate = watermark.AddHours(1), Description = "Duplicate" };

            _settingsServiceMock.Setup(s => s.GetSettings()).Returns(settings);
            _activityServiceMock
                .Setup(s => s.GetActivitiesUpdatedAfter(watermark))
                .ReturnsAsync(new List<JiraActivity> { activity });
            _repositoryMock
                .Setup(r => r.TryAppendAsync(activity, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false); // Already exists

            // When
            var result = await _tracker.RecognizeActivitiesAsync();

            // Then
            result.Should().Be(0);
        }

        [Fact]
        public async Task WhenRecognizingActivities_ThenWatermarkIsUpdated()
        {
            // Given
            var watermark = DateTime.UtcNow.AddDays(-1);
            var settings = new JiraSettings { ApiUrl = "https://test.com", ApiKey = "test-key", LastSyncTimestamp = watermark };
            
            _settingsServiceMock.Setup(s => s.GetSettings()).Returns(settings);
            _activityServiceMock
                .Setup(s => s.GetActivitiesUpdatedAfter(It.IsAny<DateTime>()))
                .ReturnsAsync(new List<JiraActivity>());

            // When
            await _tracker.RecognizeActivitiesAsync();

            // Then
            _settingsServiceMock.Verify(s => s.UpdateLastSyncTimestamp(It.IsAny<DateTime>()), Times.Once);
            _settingsServiceMock.Verify(s => s.PersistSettings(), Times.Once);
        }

        [Fact]
        public async Task GivenDeduplicationMismatch_WhenActivityNewByGuidButOldByTimestamp_ThenLogsWarning()
        {
            // Given - Activity is NEW by GUID but BEFORE watermark
            var watermark = DateTime.UtcNow.AddDays(-1);
            var settings = new JiraSettings { ApiUrl = "https://test.com", ApiKey = "test-key", LastSyncTimestamp = watermark };
            var activity = new JiraActivity 
            { 
                UpstreamId = "jira-mismatch", 
                OccurrenceDate = watermark.AddHours(-1), // Before watermark
                Description = "Mismatch" 
            };

            _settingsServiceMock.Setup(s => s.GetSettings()).Returns(settings);
            _activityServiceMock
                .Setup(s => s.GetActivitiesUpdatedAfter(watermark))
                .ReturnsAsync(new List<JiraActivity> { activity });
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
            var expectedActivities = new List<JiraActivity>
            {
                new() { UpstreamId = "jira-1", OccurrenceDate = DateTime.Today.AddDays(-3), Description = "Activity 1" },
                new() { UpstreamId = "jira-2", OccurrenceDate = DateTime.Today.AddDays(-1), Description = "Activity 2" }
            };

            _repositoryMock
                .Setup(r => r.GetActivitiesAsync(fromDate, toDate, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedActivities);

            // When
            var result = await _tracker.GetActivitiesAsync(fromDate, toDate);

            // Then
            result.Should().BeEquivalentTo(expectedActivities);
            _repositoryMock.Verify(
                r => r.GetActivitiesAsync(fromDate, toDate, It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
