using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using TrackYourDay.Core;
using TrackYourDay.Core.ApplicationTrackers.GitLab;
using TrackYourDay.Core.ApplicationTrackers.GitLab.PublicEvents;
using TrackYourDay.Core.Settings;

namespace TrackYourDay.Tests.ApplicationTrackers.GitLab
{
    [Trait("Category", "Unit")]
    public class GitLabTrackerTests
    {
        private Mock<IGitLabActivityService> gitLabActivityServiceMock;
        private Mock<IClock> clockMock;
        private Mock<IPublisher> publisherMock;
        private Mock<IGenericSettingsService> settingsServiceMock;
        private Mock<ILogger<GitLabTracker>> loggerMock;
        private GitLabTracker gitLabTracker;

        public GitLabTrackerTests()
        {
            this.gitLabActivityServiceMock = new Mock<IGitLabActivityService>();
            this.clockMock = new Mock<IClock>();
            this.publisherMock = new Mock<IPublisher>();
            this.settingsServiceMock = new Mock<IGenericSettingsService>();
            this.loggerMock = new Mock<ILogger<GitLabTracker>>();

            this.clockMock.Setup(c => c.Now).Returns(new DateTime(2025, 03, 16, 12, 0, 0));

            this.gitLabTracker = new GitLabTracker(
                this.gitLabActivityServiceMock.Object,
                this.clockMock.Object,
                this.publisherMock.Object,
                this.settingsServiceMock.Object,
                this.loggerMock.Object);
        }

        [Fact]
        public async Task GivenNoActivitiesExist_WhenRecognizingActivity_ThenNoEventsArePublished()
        {
            // Given
            this.gitLabActivityServiceMock
                .Setup(s => s.GetTodayActivitiesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<GitLabActivity>());
            this.settingsServiceMock
                .Setup(s => s.GetSetting<DateTime>(It.IsAny<string>(), It.IsAny<DateTime>()))
                .Returns(DateTime.MinValue);

            // When
            await this.gitLabTracker.RecognizeActivity();

            // Then
            this.publisherMock.Verify(
                p => p.Publish(It.IsAny<GitLabActivityDiscoveredEvent>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GivenNewActivitiesExist_WhenRecognizingActivity_ThenEventsArePublishedForEachActivity()
        {
            // Given
            var activity1 = new GitLabActivity { UpstreamId = "gitlab-1", OccuranceDate = new DateTime(2025, 03, 16, 10, 0, 0), Description = "Opened Issue: Test issue" };
            var activity2 = new GitLabActivity { UpstreamId = "gitlab-2", OccuranceDate = new DateTime(2025, 03, 16, 11, 0, 0), Description = "Closed Issue: Test issue" };
            
            this.gitLabActivityServiceMock
                .Setup(s => s.GetTodayActivitiesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<GitLabActivity> { activity1, activity2 });
            this.settingsServiceMock
                .Setup(s => s.GetSetting<DateTime>(It.IsAny<string>(), It.IsAny<DateTime>()))
                .Returns(DateTime.MinValue);

            // When
            await this.gitLabTracker.RecognizeActivity();

            // Then
            this.publisherMock.Verify(
                p => p.Publish(It.IsAny<GitLabActivityDiscoveredEvent>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        [Fact]
        public async Task GivenActivitiesAlreadyProcessed_WhenRecognizingActivity_ThenNoNewEventsArePublished()
        {
            // Given
            var activity = new GitLabActivity { UpstreamId = "gitlab-3", OccuranceDate = new DateTime(2025, 03, 16, 10, 0, 0), Description = "Opened Issue: Test issue" };
            var lastFetchTimestamp = new DateTime(2025, 03, 16, 11, 0, 0);

            this.gitLabActivityServiceMock
                .Setup(s => s.GetTodayActivitiesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<GitLabActivity> { activity });
            this.settingsServiceMock
                .Setup(s => s.GetSetting<DateTime>(It.IsAny<string>(), It.IsAny<DateTime>()))
                .Returns(lastFetchTimestamp);

            // When
            await this.gitLabTracker.RecognizeActivity();

            // Then
            this.publisherMock.Verify(
                p => p.Publish(It.IsAny<GitLabActivityDiscoveredEvent>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GivenMixOfOldAndNewActivities_WhenRecognizingActivity_ThenOnlyNewActivitiesArePublished()
        {
            // Given
            var oldActivity = new GitLabActivity { UpstreamId = "gitlab-old", OccuranceDate = new DateTime(2025, 03, 16, 9, 0, 0), Description = "Old Activity" };
            var newActivity1 = new GitLabActivity { UpstreamId = "gitlab-new1", OccuranceDate = new DateTime(2025, 03, 16, 10, 30, 0), Description = "New Activity 1" };
            var newActivity2 = new GitLabActivity { UpstreamId = "gitlab-new2", OccuranceDate = new DateTime(2025, 03, 16, 11, 0, 0), Description = "New Activity 2" };
            var lastFetchTimestamp = new DateTime(2025, 03, 16, 10, 0, 0);

            this.gitLabActivityServiceMock
                .Setup(s => s.GetTodayActivitiesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<GitLabActivity> { oldActivity, newActivity1, newActivity2 });
            this.settingsServiceMock
                .Setup(s => s.GetSetting<DateTime>(It.IsAny<string>(), It.IsAny<DateTime>()))
                .Returns(lastFetchTimestamp);

            // When
            await this.gitLabTracker.RecognizeActivity();

            // Then
            this.publisherMock.Verify(
                p => p.Publish(It.IsAny<GitLabActivityDiscoveredEvent>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        [Fact]
        public async Task GivenNewActivitiesArePublished_WhenRecognizingActivity_ThenLastFetchTimestampIsUpdated()
        {
            // Given
            var activity1 = new GitLabActivity { UpstreamId = "gitlab-6", OccuranceDate = new DateTime(2025, 03, 16, 10, 0, 0), Description = "Activity 1" };
            var activity2 = new GitLabActivity { UpstreamId = "gitlab-7", OccuranceDate = new DateTime(2025, 03, 16, 11, 0, 0), Description = "Activity 2" };
            
            this.gitLabActivityServiceMock
                .Setup(s => s.GetTodayActivitiesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<GitLabActivity> { activity1, activity2 });
            this.settingsServiceMock
                .Setup(s => s.GetSetting<DateTime>(It.IsAny<string>(), It.IsAny<DateTime>()))
                .Returns(DateTime.MinValue);

            // When
            await this.gitLabTracker.RecognizeActivity();

            // Then
            this.settingsServiceMock.Verify(
                s => s.SetSetting(It.IsAny<string>(), new DateTime(2025, 03, 16, 11, 0, 0)),
                Times.Once);
            this.settingsServiceMock.Verify(s => s.PersistSettings(), Times.Once);
        }

        [Fact]
        public async Task GivenNoNewActivities_WhenRecognizingActivity_ThenLastFetchTimestampIsNotUpdated()
        {
            // Given
            this.gitLabActivityServiceMock
                .Setup(s => s.GetTodayActivitiesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<GitLabActivity>());
            this.settingsServiceMock
                .Setup(s => s.GetSetting<DateTime>(It.IsAny<string>(), It.IsAny<DateTime>()))
                .Returns(new DateTime(2025, 03, 16, 10, 0, 0));

            // When
            await this.gitLabTracker.RecognizeActivity();

            // Then
            this.settingsServiceMock.Verify(
                s => s.SetSetting(It.IsAny<string>(), It.IsAny<DateTime>()),
                Times.Never);
            this.settingsServiceMock.Verify(s => s.PersistSettings(), Times.Never);
        }

        [Fact]
        public async Task GivenActivitiesArePublished_WhenGettingActivities_ThenPublishedActivitiesAreReturned()
        {
            // Given
            var activity1 = new GitLabActivity { UpstreamId = "gitlab-8", OccuranceDate = new DateTime(2025, 03, 16, 10, 0, 0), Description = "Activity 1" };
            var activity2 = new GitLabActivity { UpstreamId = "gitlab-9", OccuranceDate = new DateTime(2025, 03, 16, 11, 0, 0), Description = "Activity 2" };
            
            this.gitLabActivityServiceMock
                .Setup(s => s.GetTodayActivitiesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<GitLabActivity> { activity1, activity2 });
            this.settingsServiceMock
                .Setup(s => s.GetSetting<DateTime>(It.IsAny<string>(), It.IsAny<DateTime>()))
                .Returns(DateTime.MinValue);

            await this.gitLabTracker.RecognizeActivity();

            // When
            var activities = this.gitLabTracker.GetGitLabActivities();

            // Then
            activities.Should().HaveCount(2);
            activities.Should().Contain(activity1);
            activities.Should().Contain(activity2);
        }

        [Fact]
        public async Task GivenActivitiesArePublishedInMultipleCalls_WhenGettingActivities_ThenAllPublishedActivitiesAreReturned()
        {
            // Given - First call with activities
            var activity1 = new GitLabActivity { UpstreamId = "gitlab-10", OccuranceDate = new DateTime(2025, 03, 16, 10, 0, 0), Description = "Activity 1" };
            this.gitLabActivityServiceMock
                .Setup(s => s.GetTodayActivitiesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<GitLabActivity> { activity1 });
            this.settingsServiceMock
                .Setup(s => s.GetSetting<DateTime>(It.IsAny<string>(), It.IsAny<DateTime>()))
                .Returns(DateTime.MinValue);

            await this.gitLabTracker.RecognizeActivity();

            // Given - Second call with new activities
            var activity2 = new GitLabActivity { UpstreamId = "gitlab-11", OccuranceDate = new DateTime(2025, 03, 16, 11, 0, 0), Description = "Activity 2" };
            this.gitLabActivityServiceMock
                .Setup(s => s.GetTodayActivitiesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<GitLabActivity> { activity1, activity2 });
            this.settingsServiceMock
                .Setup(s => s.GetSetting<DateTime>(It.IsAny<string>(), It.IsAny<DateTime>()))
                .Returns(new DateTime(2025, 03, 16, 10, 0, 0));

            await this.gitLabTracker.RecognizeActivity();

            // When
            var activities = this.gitLabTracker.GetGitLabActivities();

            // Then
            activities.Should().HaveCount(2);
            activities.Should().Contain(activity1);
            activities.Should().Contain(activity2);
        }

        [Fact]
        public async Task GivenActivitiesWithSameTimestamp_WhenRecognizingActivity_ThenActivitiesAfterLastFetchArePublished()
        {
            // Given
            var activity1 = new GitLabActivity { UpstreamId = "gitlab-12", OccuranceDate = new DateTime(2025, 03, 16, 10, 0, 0), Description = "Activity 1" };
            var activity2 = new GitLabActivity { UpstreamId = "gitlab-13", OccuranceDate = new DateTime(2025, 03, 16, 10, 0, 0), Description = "Activity 2" };
            var lastFetchTimestamp = new DateTime(2025, 03, 16, 10, 0, 0);

            this.gitLabActivityServiceMock
                .Setup(s => s.GetTodayActivitiesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<GitLabActivity> { activity1, activity2 });
            this.settingsServiceMock
                .Setup(s => s.GetSetting<DateTime>(It.IsAny<string>(), It.IsAny<DateTime>()))
                .Returns(lastFetchTimestamp);

            // When
            await this.gitLabTracker.RecognizeActivity();

            // Then - Activities with same timestamp as last fetch should NOT be published
            this.publisherMock.Verify(
                p => p.Publish(It.IsAny<GitLabActivityDiscoveredEvent>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GivenNoStoredTimestamp_WhenRecognizingActivity_ThenUsesLast7DaysAsDefault()
        {
            // Given - Clock is set to 2025-03-16 12:00:00
            // Default should be 7 days before: 2025-03-09 12:00:00
            var activityWithin7Days = new GitLabActivity { UpstreamId = "gitlab-14", OccuranceDate = new DateTime(2025, 03, 10, 10, 0, 0), Description = "Activity within 7 days" };
            var activityBefore7Days = new GitLabActivity { UpstreamId = "gitlab-15", OccuranceDate = new DateTime(2025, 03, 09, 10, 0, 0), Description = "Activity before 7 days" };
            
            this.gitLabActivityServiceMock
                .Setup(s => s.GetTodayActivitiesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<GitLabActivity> { activityBefore7Days, activityWithin7Days });
            
            // Mock the settings service to use the default value (7 days ago)
            this.settingsServiceMock
                .Setup(s => s.GetSetting<DateTime>(It.IsAny<string>(), It.IsAny<DateTime>()))
                .Returns((string key, DateTime defaultValue) => defaultValue);

            // When
            await this.gitLabTracker.RecognizeActivity();

            // Then - Only activity within 7 days should be published (> 7 days ago)
            this.publisherMock.Verify(
                p => p.Publish(It.IsAny<GitLabActivityDiscoveredEvent>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
