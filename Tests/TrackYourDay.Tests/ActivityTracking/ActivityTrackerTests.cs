using FluentAssertions;
using MediatR;
using Moq;
using TrackYourDay.Tests.Activities;

namespace TrackYourDay.Tests.ActivityTracking
{
    [Trait("Category", "Unit")]
    public class ActivityTrackerTests
    {
        private Mock<IPublisher> publisherMock;
        private Mock<IStartedActivityRecognizingStrategy> startedActivityRecognizingStrategy;
        private Mock<IInstantActivityRecognizingStrategy> instantActivityRecognizingStrategy;
        private ActivityTracker activityEventTracker;

        public ActivityTrackerTests()
        {
            this.publisherMock = new Mock<IPublisher>();
            this.startedActivityRecognizingStrategy = new Mock<IStartedActivityRecognizingStrategy>();
            this.instantActivityRecognizingStrategy = new Mock<IInstantActivityRecognizingStrategy>();

            this.activityEventTracker = new ActivityTracker(
                this.publisherMock.Object, 
                this.startedActivityRecognizingStrategy.Object,
                this.instantActivityRecognizingStrategy.Object);
        }

        [Fact]
        public void WhenNewPeriodicActivityIsStarted_ThenPeriodicActivityStartedEventIsPublished()
        {
            // Arrange
            this.startedActivityRecognizingStrategy.Setup(s => s.RecognizeActivity())
                .Returns(ActivityTypeFactory.FocusOnApplicationActivityType("Application"));

            // Act
            activityEventTracker.RecognizeEvents();

            // Assert
            publisherMock.Verify(x => x.Publish(It.IsAny<PeriodicActivityStartedNotification>(), CancellationToken.None), Times.Once);
        }

        [Fact]
        public void GivenThereIsPeriodActivityStarted_WhenNewPeriodicActivityIsStarted_ThenPeriodicActivityEndedEventIsPublished()
        {
            // Arrange
            this.startedActivityRecognizingStrategy.Setup(s => s.RecognizeActivity())
                .Returns(ActivityTypeFactory.FocusOnApplicationActivityType("Application"));
            this.activityEventTracker.RecognizeEvents();
            this.startedActivityRecognizingStrategy.Setup(s => s.RecognizeActivity())
                .Returns(ActivityTypeFactory.FocusOnApplicationActivityType("Another Application"));

            // Act
            this.activityEventTracker.RecognizeEvents();

            // Assert
            this.publisherMock.Verify(x => x.Publish(It.IsAny<PeriodicActivityEndedNotification >(), CancellationToken.None), Times.Once);
        }

        [Fact]
        public void GivenThereIsPeriodActivityStarted_WhenNewPeriodicActivityIsStarted_ThenPeriodicActivityStartedEventIsPublished()
        {
            // Arrange
            this.startedActivityRecognizingStrategy.Setup(s => s.RecognizeActivity())
                .Returns(ActivityTypeFactory.FocusOnApplicationActivityType("Application"));

            // Act
            activityEventTracker.RecognizeEvents();

            // Assert
            publisherMock.Verify(x => x.Publish(It.IsAny<PeriodicActivityStartedNotification>(), CancellationToken.None), Times.Once);
        }

        [Fact]
        public void GivenThereIsPeriodActivityStarted_WhenNothingChanges_ThenAnyEventIsNotPublished()
        {
            // Arrange
            this.startedActivityRecognizingStrategy.Setup(s => s.RecognizeActivity())
                .Returns(ActivityTypeFactory.FocusOnApplicationActivityType("Application"));
            this.activityEventTracker.RecognizeEvents();
            var existingNotificationsCount = this.publisherMock.Invocations.Count;

            // Act
            this.activityEventTracker.RecognizeEvents();

            // Assert
            this.publisherMock.Invocations.Count.Should().Be(existingNotificationsCount);
        }

        [Fact]
        public void WhenInstantPeriodicActivityIsRecognized_ThenInstantActivityOccuredEventIsPublished()
        {
            // Arrange
            this.instantActivityRecognizingStrategy.Setup(s => s.RecognizeActivity())
                .Returns(ActivityTypeFactory.MouseMovedActivityType(0,0));

            // Act
            activityEventTracker.RecognizeEvents();

            // Assert
            publisherMock.Verify(x => x.Publish(It.IsAny<PeriodicActivityStartedNotification>(), CancellationToken.None), Times.Once);
        }

        public void WhenNewPeriodicActivityIsStarted_ThenItIsCurrentActivity() 
        {
            // Arrange
            var activity = ActivityTypeFactory.FocusOnApplicationActivityType("Application");
            this.startedActivityRecognizingStrategy.Setup(s => s.RecognizeActivity())
                .Returns(activity);

            // Act
            activityEventTracker.RecognizeEvents();

            // Assert
            activityEventTracker.GetCurrentActivity().Should().Be(activity);
        }

        public void WhenNewPeriodicActivityIsEnded_ThenItIsAddedToAllActivitiesList()
        {
            // Arrange
            var activity = ActivityTypeFactory.FocusOnApplicationActivityType("Application");
            this.startedActivityRecognizingStrategy.Setup(s => s.RecognizeActivity())
                .Returns(activity);

            // Act
            activityEventTracker.RecognizeEvents();

            // Assert
            activityEventTracker.GetEndedActivities().LastOrDefault().Should().Be(activity);
        }
    }
}