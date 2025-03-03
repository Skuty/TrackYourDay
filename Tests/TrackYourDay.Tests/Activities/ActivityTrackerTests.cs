using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using TrackYourDay.Core;
using TrackYourDay.Core.SystemTrackers;
using TrackYourDay.Core.SystemTrackers.ActivityRecognizing;
using TrackYourDay.Core.SystemTrackers.Events;
using TrackYourDay.Core.SystemTrackers.SystemStates;

namespace TrackYourDay.Tests.Activities
{
    [Trait("Category", "Unit")]
    public class ActivityTrackerTests
    {
        private IClock clock;
        private Mock<IPublisher> publisherMock;
        private Mock<ILogger<ActivityTracker>> loggerMock;
        private Mock<ISystemStateRecognizingStrategy> startedActivityRecognizingStrategy;
        private Mock<ISystemStateRecognizingStrategy> mousePositionRecognizingStrategy;
        private Mock<ISystemStateRecognizingStrategy> lastInputRecognizingStrategy;
        private ActivityTracker activityEventTracker;

        public ActivityTrackerTests()
        {
            this.clock = new Clock();
            this.loggerMock = new Mock<ILogger<ActivityTracker>>();
            this.publisherMock = new Mock<IPublisher>();
            this.startedActivityRecognizingStrategy = new Mock<ISystemStateRecognizingStrategy>();
            this.mousePositionRecognizingStrategy = new Mock<ISystemStateRecognizingStrategy>();
            this.lastInputRecognizingStrategy = new Mock<ISystemStateRecognizingStrategy>();

            this.activityEventTracker = new ActivityTracker(
                this.clock,
                this.publisherMock.Object,
                this.startedActivityRecognizingStrategy.Object,
                this.mousePositionRecognizingStrategy.Object,
                this.lastInputRecognizingStrategy.Object,
                this.loggerMock.Object);
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

        //TODO Remove those two tests and adjust implementation - ActivityTracker should be generic and not aware of used activities
        // This one can also be removed as its functionlity is covered by LastInput activity
        //[Fact]
        //public void GivenMousePositionChanged_WhenInstantPeriodicActivityIsRecognized_ThenInstantActivityOccuredEventIsPublished()
        //{
        //    // Arrange
        //    var mouseMovedState = SystemStateFactory.MouseMouvedEvent(0, 0);
        //    this.mousePositionRecognizingStrategy.Setup(s => s.RecognizeActivity())
        //        .Returns(mouseMovedState);

        //    // Act
        //    this.activityEventTracker.RecognizeActivity();

        //    // Assert
        //    this.publisherMock.Verify(x => x.Publish(It.Is<InstantActivityOccuredEvent>(a => a.InstantActivity.SystemState == mouseMovedState), CancellationToken.None), Times.Once);
        //}

        [Fact(Skip = "Temporary disabled until fix or configuration form UI will be available")]
        public void GivenLastInputChanged_WhenInstantActivityIsRecognized_ThenInstantActivityOccuredEventIsPublished()
        {
            // Arrange
            var lastInputSystemState = SystemStateFactory.LastInputState(new DateTime(2024, 01, 01));
            this.lastInputRecognizingStrategy.Setup(s => s.RecognizeActivity())
                .Returns(lastInputSystemState); 

            // Act
            this.activityEventTracker.RecognizeActivity();

            // Assert
            this.publisherMock.Verify(x => x.Publish(It.Is<InstantActivityOccuredEvent>(a => a.InstantActivity.SystemState == lastInputSystemState), CancellationToken.None), Times.Once);
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