﻿using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using TrackYourDay.Core;
using TrackYourDay.Core.Activities;
using TrackYourDay.Core.Activities.ActivityRecognizing;
using TrackYourDay.Core.Activities.Notifications;
using TrackYourDay.Core.Activities.SystemStates;

namespace TrackYourDay.Tests.ActivityTracking
{
    [Trait("Category", "Unit")]
    public class ActivityTrackerTests
    {
        private Core.IClock clock;
        private Mock<IPublisher> publisherMock;
        private Mock<ILogger<ActivityTracker>> loggerMock;
        private Mock<ISystemStateRecognizingStrategy> startedActivityRecognizingStrategy;
        private Mock<ISystemStateRecognizingStrategy> instantActivityRecognizingStrategy;
        private ActivityTracker activityEventTracker;

        public ActivityTrackerTests()
        {
            this.clock = new Clock();
            this.loggerMock = new Mock<ILogger<ActivityTracker>>();
            this.publisherMock = new Mock<IPublisher>();
            this.startedActivityRecognizingStrategy = new Mock<ISystemStateRecognizingStrategy>();
            this.instantActivityRecognizingStrategy = new Mock<ISystemStateRecognizingStrategy>();

            this.activityEventTracker = new ActivityTracker(
                this.clock,
                this.publisherMock.Object,
                this.startedActivityRecognizingStrategy.Object,
                this.instantActivityRecognizingStrategy.Object,
                this.loggerMock.Object);
        }

        [Fact]
        public void WhenNewPeriodicActivityIsStarted_ThenPeriodicActivityStartedEventIsPublished()
        {
            // Arrange
            this.startedActivityRecognizingStrategy.Setup(s => s.RecognizeActivity())
                .Returns(SystemStateFactory.FocusOnApplicationState("Application"));

            // Act
            activityEventTracker.RecognizeActivity();

            // Assert
            publisherMock.Verify(x => x.Publish(It.IsAny<PeriodicActivityStartedNotification>(), CancellationToken.None), Times.Once);
        }

        [Fact]
        public void GivenThereIsPeriodActivityStarted_WhenNewPeriodicActivityIsStarted_ThenPeriodicActivityEndedEventIsPublished()
        {
            // Arrange
            this.startedActivityRecognizingStrategy.Setup(s => s.RecognizeActivity())
                .Returns(SystemStateFactory.FocusOnApplicationState("Another Application"));

            // Act
            this.activityEventTracker.RecognizeActivity();

            // Assert
            this.publisherMock.Verify(x => x.Publish(It.IsAny<PeriodicActivityEndedNotification >(), CancellationToken.None), Times.Once);
        }

        [Fact]
        public void GivenThereIsPeriodActivityStarted_WhenNewPeriodicActivityIsStarted_ThenPeriodicActivityStartedEventIsPublished()
        {
            // Arrange
            this.startedActivityRecognizingStrategy.Setup(s => s.RecognizeActivity())
                .Returns(SystemStateFactory.FocusOnApplicationState("Application"));

            // Act
            activityEventTracker.RecognizeActivity();

            // Assert
            publisherMock.Verify(x => x.Publish(It.IsAny<PeriodicActivityStartedNotification>(), CancellationToken.None), Times.Once);
        }

        [Fact]
        public void GivenThereIsPeriodActivityStarted_WhenNothingChanges_ThenAnyEventIsNotPublished()
        {
            // Arrange
            this.startedActivityRecognizingStrategy.Setup(s => s.RecognizeActivity())
                .Returns(SystemStateFactory.FocusOnApplicationState("Application"));
            this.activityEventTracker.RecognizeActivity();
            var existingNotificationsCount = this.publisherMock.Invocations.Count;

            // Act
            this.activityEventTracker.RecognizeActivity();

            // Assert
            this.publisherMock.Invocations.Count.Should().Be(existingNotificationsCount);
        }

        [Fact]
        public void WhenInstantPeriodicActivityIsRecognized_ThenInstantActivityOccuredEventIsPublished()
        {
            // Arrange
            this.instantActivityRecognizingStrategy.Setup(s => s.RecognizeActivity())
                .Returns(SystemStateFactory.MouseMouvedEvent(0,0));

            // Act
            this.activityEventTracker.RecognizeActivity();

            // Assert
            this.publisherMock.Verify(x => x.Publish(It.IsAny<PeriodicActivityStartedNotification>(), CancellationToken.None), Times.Once);
        }

        [Fact]
        public void WhenNewPeriodicActivityIsStarted_ThenItIsCurrentActivity() 
        {
            // Arrange
            var activity = SystemStateFactory.FocusOnApplicationState("Application");
            this.startedActivityRecognizingStrategy.Setup(s => s.RecognizeActivity())
                .Returns(activity);

            // Act
            this.activityEventTracker.RecognizeActivity();

            // Assert
            this.activityEventTracker.GetCurrentActivity().SystemState.Should().Be(activity);
        }

        [Fact]
        public void WhenNewPeriodicActivityIsEnded_ThenItIsAddedToAllActivitiesList()
        {
            // Arrange
            var activity = SystemStateFactory.FocusOnApplicationState("Application");
            this.startedActivityRecognizingStrategy.Setup(s => s.RecognizeActivity())
                .Returns(activity);
            this.activityEventTracker.RecognizeActivity();
            this.startedActivityRecognizingStrategy.Setup(s => s.RecognizeActivity())
                .Returns(SystemStateFactory.FocusOnApplicationState("New Application"));

            // Act
            this.activityEventTracker.RecognizeActivity();

            // Assert
            this.activityEventTracker.GetEndedActivities().LastOrDefault().ActivityType.Should().Be(activity);
        }
    }
}