using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using TrackYourDay.Core;
using TrackYourDay.Core.Activities;
using TrackYourDay.Core.Activities.ActivityRecognizing;
using TrackYourDay.Core.Activities.Events;
using TrackYourDay.Core.Activities.SystemStates;

namespace TrackYourDay.Tests.Activities
{
    [Trait("Category", "Unit")]
    public class ActivityTrackerTests
    {
        private IClock clock;
        private Mock<IPublisher> publisherMock;
        private Mock<ILogger<ActivityTracker>> loggerMock;
        private Mock<ISystemStateRecognizingStrategy> startedActivityRecognizingStrategy;
        private Mock<ISystemStateRecognizingStrategy> instantActivityRecognizingStrategy;
        private ActivityTracker activityEventTracker;

        public ActivityTrackerTests()
        {
            clock = new Clock();
            loggerMock = new Mock<ILogger<ActivityTracker>>();
            publisherMock = new Mock<IPublisher>();
            startedActivityRecognizingStrategy = new Mock<ISystemStateRecognizingStrategy>();
            instantActivityRecognizingStrategy = new Mock<ISystemStateRecognizingStrategy>();

            activityEventTracker = new ActivityTracker(
                clock,
                publisherMock.Object,
                startedActivityRecognizingStrategy.Object,
                instantActivityRecognizingStrategy.Object,
                loggerMock.Object);
        }

        [Fact]
        public void WhenNewPeriodicActivityIsStarted_ThenPeriodicActivityStartedEventIsPublished()
        {
            // Arrange
            startedActivityRecognizingStrategy.Setup(s => s.RecognizeActivity())
                .Returns(SystemStateFactory.FocusOnApplicationState("Application"));

            // Act
            activityEventTracker.RecognizeActivity();

            // Assert
            publisherMock.Verify(x => x.Publish(It.IsAny<PeriodicActivityStartedEvent>(), CancellationToken.None), Times.Once);
        }

        [Fact]
        public void GivenThereIsPeriodActivityStarted_WhenNewPeriodicActivityIsStarted_ThenPeriodicActivityEndedEventIsPublished()
        {
            // Arrange
            startedActivityRecognizingStrategy.Setup(s => s.RecognizeActivity())
                .Returns(SystemStateFactory.FocusOnApplicationState("Another Application"));

            // Act
            activityEventTracker.RecognizeActivity();

            // Assert
            publisherMock.Verify(x => x.Publish(It.IsAny<PeriodicActivityEndedEvent>(), CancellationToken.None), Times.Once);
        }

        [Fact]
        public void GivenThereIsPeriodActivityStarted_WhenNewPeriodicActivityIsStarted_ThenPeriodicActivityStartedEventIsPublished()
        {
            // Arrange
            startedActivityRecognizingStrategy.Setup(s => s.RecognizeActivity())
                .Returns(SystemStateFactory.FocusOnApplicationState("Application"));

            // Act
            activityEventTracker.RecognizeActivity();

            // Assert
            publisherMock.Verify(x => x.Publish(It.IsAny<PeriodicActivityStartedEvent>(), CancellationToken.None), Times.Once);
        }

        [Fact]
        public void GivenThereIsPeriodActivityStarted_WhenNothingChanges_ThenAnyEventIsNotPublished()
        {
            // Arrange
            startedActivityRecognizingStrategy.Setup(s => s.RecognizeActivity())
                .Returns(SystemStateFactory.FocusOnApplicationState("Application"));
            activityEventTracker.RecognizeActivity();
            var existingNotificationsCount = publisherMock.Invocations.Count;

            // Act
            activityEventTracker.RecognizeActivity();

            // Assert
            publisherMock.Invocations.Count.Should().Be(existingNotificationsCount);
        }

        [Fact]
        public void WhenInstantPeriodicActivityIsRecognized_ThenInstantActivityOccuredEventIsPublished()
        {
            // Arrange
            instantActivityRecognizingStrategy.Setup(s => s.RecognizeActivity())
                .Returns(SystemStateFactory.MouseMouvedEvent(0, 0));

            // Act
            activityEventTracker.RecognizeActivity();

            // Assert
            publisherMock.Verify(x => x.Publish(It.IsAny<PeriodicActivityStartedEvent>(), CancellationToken.None), Times.Once);
        }

        [Fact]
        public void WhenNewPeriodicActivityIsStarted_ThenItIsCurrentActivity()
        {
            // Arrange
            var activity = SystemStateFactory.FocusOnApplicationState("Application");
            startedActivityRecognizingStrategy.Setup(s => s.RecognizeActivity())
                .Returns(activity);

            // Act
            activityEventTracker.RecognizeActivity();

            // Assert
            activityEventTracker.GetCurrentActivity().SystemState.Should().Be(activity);
        }

        [Fact]
        public void WhenNewPeriodicActivityIsEnded_ThenItIsAddedToAllActivitiesList()
        {
            // Arrange
            var activity = SystemStateFactory.FocusOnApplicationState("Application");
            startedActivityRecognizingStrategy.Setup(s => s.RecognizeActivity())
                .Returns(activity);
            activityEventTracker.RecognizeActivity();
            startedActivityRecognizingStrategy.Setup(s => s.RecognizeActivity())
                .Returns(SystemStateFactory.FocusOnApplicationState("New Application"));

            // Act
            activityEventTracker.RecognizeActivity();

            // Assert
            activityEventTracker.GetEndedActivities().LastOrDefault().ActivityType.Should().Be(activity);
        }
    }
}