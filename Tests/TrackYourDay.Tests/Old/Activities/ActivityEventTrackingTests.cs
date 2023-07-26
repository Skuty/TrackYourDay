using FluentAssertions;
using MediatR;
using Moq;
using TrackYourDay.Core.Activities;
using TrackYourDay.Core.Old.Activities;
using TrackYourDay.Core.Old.Activities.Notifications;
using TrackYourDay.Core.Old.Activities.RecognizingStrategies;

namespace TrackYourDay.Tests.Old.Activities
{
    public class ActivityEventTrackingTests
    {
        private Mock<IPublisher> publisherMock;

        public ActivityEventTrackingTests()
        {
            publisherMock = new Mock<IPublisher>();
        }

        [Fact]
        public void GivenNoEventsAreRegistered_WhenNewActivityIsRecognized_ThenEventIsRegistered()
        {
            // Arrange
            var eventRecognizingStrategy = new AlwaysNewActivityRecognizingStrategy();
            var eventRecognizer = new ActivityEventTracker(publisherMock.Object, eventRecognizingStrategy);
            var existingEventsCounts = 0;

            // Act
            eventRecognizer.RecognizeEvents();

            // Assert
            eventRecognizer.GetRegisteredActivities().Count.Should().Be(existingEventsCounts + 1);
        }

        [Fact]
        public void WhenNewActivityIsRecognized_ThenEventIsRegistered()
        {
            // Arrange
            var eventRecognizingStrategy = new AlwaysNewActivityRecognizingStrategy();
            var eventRecognizer = new ActivityEventTracker(publisherMock.Object, eventRecognizingStrategy);
            eventRecognizer.RecognizeEvents();
            var existingEventsCounts = eventRecognizer.GetRegisteredActivities().Count;

            // Act
            eventRecognizer.RecognizeEvents();

            // Assert
            eventRecognizer.GetRegisteredActivities().Count.Should().Be(existingEventsCounts + 1);
        }

        [Fact]
        public void WhenNotChangedActivityIsRecognized_ThenEventIsNotRegistered()
        {
            // Arrange
            var eventRecognizingStrategy = new AlwaysTheSameActivityRecognizingStrategy();
            var eventRecognizer = new ActivityEventTracker(publisherMock.Object, eventRecognizingStrategy);
            eventRecognizer.RecognizeEvents();
            var existingEventsCounts = eventRecognizer.GetRegisteredActivities().Count;

            // Act
            eventRecognizer.RecognizeEvents();

            // Assert
            eventRecognizer.GetRegisteredActivities().Count.Should().Be(existingEventsCounts);
        }

        [Fact]
        public void WhenEventIsRegistered_ThenNewEventNotificationIsSent()
        {
            // Arrange
            var eventRecognizingStrategy = new AlwaysNewActivityRecognizingStrategy();
            var eventRecognizer = new ActivityEventTracker(publisherMock.Object, eventRecognizingStrategy);

            // Act
            eventRecognizer.RecognizeEvents();

            // Assert
            publisherMock.Verify(x => x.Publish(It.IsAny<ActivityEventRecognizedNotification>(), CancellationToken.None), Times.Once);
        }

        private record class DummyActivity(string name) : Activity(name);

        private class AlwaysNewActivityRecognizingStrategy : IActivityRecognizingStrategy
        {
            public Activity RecognizeActivity()
            {
                return new DummyActivity($"Activity with random id: {DateTime.Now.Ticks}");
            }
        }

        private class AlwaysTheSameActivityRecognizingStrategy : IActivityRecognizingStrategy
        {
            public Activity RecognizeActivity()
            {
                return new DummyActivity($"Same Activity");
            }
        }
    }
}