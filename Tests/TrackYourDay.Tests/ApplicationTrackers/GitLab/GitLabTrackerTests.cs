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
        public void GivenNoActivitiesExist_WhenRecognizingActivity_ThenNoEventsArePublished()
        {
            // Given
            this.gitLabActivityServiceMock
                .Setup(s => s.GetTodayActivities())
                .Returns(new List<GitLabActivity>());
            this.settingsServiceMock
                .Setup(s => s.GetSetting<DateTime>(It.IsAny<string>(), It.IsAny<DateTime>()))
                .Returns(DateTime.MinValue);

            // When
            this.gitLabTracker.RecognizeActivity();

            // Then
            this.publisherMock.Verify(
                p => p.Publish(It.IsAny<GitLabActivityDiscoveredEvent>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public void GivenNewActivitiesExist_WhenRecognizingActivity_ThenEventsArePublishedForEachActivity()
        {
            // Given
            var activity1 = new GitLabActivity(new DateTime(2025, 03, 16, 10, 0, 0), "Opened Issue: Test issue");
            var activity2 = new GitLabActivity(new DateTime(2025, 03, 16, 11, 0, 0), "Closed Issue: Test issue");
            
            this.gitLabActivityServiceMock
                .Setup(s => s.GetTodayActivities())
                .Returns(new List<GitLabActivity> { activity1, activity2 });
            this.settingsServiceMock
                .Setup(s => s.GetSetting<DateTime>(It.IsAny<string>(), It.IsAny<DateTime>()))
                .Returns(DateTime.MinValue);

            // When
            this.gitLabTracker.RecognizeActivity();

            // Then
            this.publisherMock.Verify(
                p => p.Publish(It.IsAny<GitLabActivityDiscoveredEvent>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        [Fact]
        public void GivenActivitiesAlreadyProcessed_WhenRecognizingActivity_ThenNoNewEventsArePublished()
        {
            // Given
            var activity = new GitLabActivity(new DateTime(2025, 03, 16, 10, 0, 0), "Opened Issue: Test issue");
            var lastFetchTimestamp = new DateTime(2025, 03, 16, 11, 0, 0);

            this.gitLabActivityServiceMock
                .Setup(s => s.GetTodayActivities())
                .Returns(new List<GitLabActivity> { activity });
            this.settingsServiceMock
                .Setup(s => s.GetSetting<DateTime>(It.IsAny<string>(), It.IsAny<DateTime>()))
                .Returns(lastFetchTimestamp);

            // When
            this.gitLabTracker.RecognizeActivity();

            // Then
            this.publisherMock.Verify(
                p => p.Publish(It.IsAny<GitLabActivityDiscoveredEvent>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public void GivenMixOfOldAndNewActivities_WhenRecognizingActivity_ThenOnlyNewActivitiesArePublished()
        {
            // Given
            var oldActivity = new GitLabActivity(new DateTime(2025, 03, 16, 9, 0, 0), "Old Activity");
            var newActivity1 = new GitLabActivity(new DateTime(2025, 03, 16, 10, 30, 0), "New Activity 1");
            var newActivity2 = new GitLabActivity(new DateTime(2025, 03, 16, 11, 0, 0), "New Activity 2");
            var lastFetchTimestamp = new DateTime(2025, 03, 16, 10, 0, 0);

            this.gitLabActivityServiceMock
                .Setup(s => s.GetTodayActivities())
                .Returns(new List<GitLabActivity> { oldActivity, newActivity1, newActivity2 });
            this.settingsServiceMock
                .Setup(s => s.GetSetting<DateTime>(It.IsAny<string>(), It.IsAny<DateTime>()))
                .Returns(lastFetchTimestamp);

            // When
            this.gitLabTracker.RecognizeActivity();

            // Then
            this.publisherMock.Verify(
                p => p.Publish(It.IsAny<GitLabActivityDiscoveredEvent>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        [Fact]
        public void GivenNewActivitiesArePublished_WhenRecognizingActivity_ThenLastFetchTimestampIsUpdated()
        {
            // Given
            var activity1 = new GitLabActivity(new DateTime(2025, 03, 16, 10, 0, 0), "Activity 1");
            var activity2 = new GitLabActivity(new DateTime(2025, 03, 16, 11, 0, 0), "Activity 2");
            
            this.gitLabActivityServiceMock
                .Setup(s => s.GetTodayActivities())
                .Returns(new List<GitLabActivity> { activity1, activity2 });
            this.settingsServiceMock
                .Setup(s => s.GetSetting<DateTime>(It.IsAny<string>(), It.IsAny<DateTime>()))
                .Returns(DateTime.MinValue);

            // When
            this.gitLabTracker.RecognizeActivity();

            // Then
            this.settingsServiceMock.Verify(
                s => s.SetSetting(It.IsAny<string>(), new DateTime(2025, 03, 16, 11, 0, 0)),
                Times.Once);
            this.settingsServiceMock.Verify(s => s.PersistSettings(), Times.Once);
        }

        [Fact]
        public void GivenNoNewActivities_WhenRecognizingActivity_ThenLastFetchTimestampIsNotUpdated()
        {
            // Given
            this.gitLabActivityServiceMock
                .Setup(s => s.GetTodayActivities())
                .Returns(new List<GitLabActivity>());
            this.settingsServiceMock
                .Setup(s => s.GetSetting<DateTime>(It.IsAny<string>(), It.IsAny<DateTime>()))
                .Returns(new DateTime(2025, 03, 16, 10, 0, 0));

            // When
            this.gitLabTracker.RecognizeActivity();

            // Then
            this.settingsServiceMock.Verify(
                s => s.SetSetting(It.IsAny<string>(), It.IsAny<DateTime>()),
                Times.Never);
            this.settingsServiceMock.Verify(s => s.PersistSettings(), Times.Never);
        }

        [Fact]
        public void GivenActivitiesArePublished_WhenGettingActivities_ThenPublishedActivitiesAreReturned()
        {
            // Given
            var activity1 = new GitLabActivity(new DateTime(2025, 03, 16, 10, 0, 0), "Activity 1");
            var activity2 = new GitLabActivity(new DateTime(2025, 03, 16, 11, 0, 0), "Activity 2");
            
            this.gitLabActivityServiceMock
                .Setup(s => s.GetTodayActivities())
                .Returns(new List<GitLabActivity> { activity1, activity2 });
            this.settingsServiceMock
                .Setup(s => s.GetSetting<DateTime>(It.IsAny<string>(), It.IsAny<DateTime>()))
                .Returns(DateTime.MinValue);

            this.gitLabTracker.RecognizeActivity();

            // When
            var activities = this.gitLabTracker.GetGitLabActivities();

            // Then
            activities.Should().HaveCount(2);
            activities.Should().Contain(activity1);
            activities.Should().Contain(activity2);
        }

        [Fact]
        public void GivenActivitiesArePublishedInMultipleCalls_WhenGettingActivities_ThenAllPublishedActivitiesAreReturned()
        {
            // Given - First call with activities
            var activity1 = new GitLabActivity(new DateTime(2025, 03, 16, 10, 0, 0), "Activity 1");
            this.gitLabActivityServiceMock
                .Setup(s => s.GetTodayActivities())
                .Returns(new List<GitLabActivity> { activity1 });
            this.settingsServiceMock
                .Setup(s => s.GetSetting<DateTime>(It.IsAny<string>(), It.IsAny<DateTime>()))
                .Returns(DateTime.MinValue);

            this.gitLabTracker.RecognizeActivity();

            // Given - Second call with new activities
            var activity2 = new GitLabActivity(new DateTime(2025, 03, 16, 11, 0, 0), "Activity 2");
            this.gitLabActivityServiceMock
                .Setup(s => s.GetTodayActivities())
                .Returns(new List<GitLabActivity> { activity1, activity2 });
            this.settingsServiceMock
                .Setup(s => s.GetSetting<DateTime>(It.IsAny<string>(), It.IsAny<DateTime>()))
                .Returns(new DateTime(2025, 03, 16, 10, 0, 0));

            this.gitLabTracker.RecognizeActivity();

            // When
            var activities = this.gitLabTracker.GetGitLabActivities();

            // Then
            activities.Should().HaveCount(2);
            activities.Should().Contain(activity1);
            activities.Should().Contain(activity2);
        }

        [Fact]
        public void GivenActivitiesWithSameTimestamp_WhenRecognizingActivity_ThenActivitiesAfterLastFetchArePublished()
        {
            // Given
            var activity1 = new GitLabActivity(new DateTime(2025, 03, 16, 10, 0, 0), "Activity 1");
            var activity2 = new GitLabActivity(new DateTime(2025, 03, 16, 10, 0, 0), "Activity 2");
            var lastFetchTimestamp = new DateTime(2025, 03, 16, 10, 0, 0);

            this.gitLabActivityServiceMock
                .Setup(s => s.GetTodayActivities())
                .Returns(new List<GitLabActivity> { activity1, activity2 });
            this.settingsServiceMock
                .Setup(s => s.GetSetting<DateTime>(It.IsAny<string>(), It.IsAny<DateTime>()))
                .Returns(lastFetchTimestamp);

            // When
            this.gitLabTracker.RecognizeActivity();

            // Then - Activities with same timestamp as last fetch should NOT be published
            this.publisherMock.Verify(
                p => p.Publish(It.IsAny<GitLabActivityDiscoveredEvent>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
