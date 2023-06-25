using FluentAssertions;
using MediatR;
using Moq;
using TrackYourDay.Core.Activities;
using TrackYourDay.Core.Activities.Notifications;
using TrackYourDay.Core.Activities.RecognizingStrategies;

namespace TrackYourDay.Tests
{
    public class ActivityEventTrackingTests
    {
        private Mock<IPublisher> publisherMock;

        public ActivityEventTrackingTests()
        {
            this.publisherMock = new Mock<IPublisher>();
        }

        [Fact]
        public void GivenNoEventsAreRegistered_WhenNewActivityIsRecognized_ThenEventIsRegistered()
        {
            // Arrange
            var eventRecognizingStrategy = new AlwaysNewActivityRecognizingStrategy();
            var eventRecognizer = new ActivityEventTracker(this.publisherMock.Object, eventRecognizingStrategy);
            var existingEventsCounts = 0;

            // Act
            eventRecognizer.RecognizeEvents();

            // Assert
            eventRecognizer.GetRegisteredEvents().Count.Should().Be(existingEventsCounts + 1);
        }

        [Fact]
        public void WhenNewActivityIsRecognized_ThenEventIsRegistered()
        {
            // Arrange
            var eventRecognizingStrategy = new AlwaysNewActivityRecognizingStrategy();
            var eventRecognizer = new ActivityEventTracker(this.publisherMock.Object, eventRecognizingStrategy);
            eventRecognizer.RecognizeEvents();
            var existingEventsCounts = eventRecognizer.GetRegisteredEvents().Count;

            // Act
            eventRecognizer.RecognizeEvents();

            // Assert
            eventRecognizer.GetRegisteredEvents().Count.Should().Be(existingEventsCounts + 1);
        }

        [Fact]
        public void WhenNotChangedActivityIsRecognized_ThenEventIsNotRegistered()
        {
            // Arrange
            var eventRecognizingStrategy = new AlwaysTheSameActivityRecognizingStrategy();
            var eventRecognizer = new ActivityEventTracker(this.publisherMock.Object, eventRecognizingStrategy);
            eventRecognizer.RecognizeEvents();
            var existingEventsCounts = eventRecognizer.GetRegisteredEvents().Count;

            // Act
            eventRecognizer.RecognizeEvents();

            // Assert
            eventRecognizer.GetRegisteredEvents().Count.Should().Be(existingEventsCounts);
        }

        [Fact]
        public void WhenEventIsRegistered_ThenNewEventNotificationIsSent()
        {
            // Arrange
            var eventRecognizingStrategy = new AlwaysNewActivityRecognizingStrategy();
            var eventRecognizer = new ActivityEventTracker(this.publisherMock.Object, eventRecognizingStrategy);

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