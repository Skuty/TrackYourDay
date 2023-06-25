using MediatR;
using Moq;
using TrackYourDay.Core;
using TrackYourDay.Core.Activities;
using TrackYourDay.Core.Tasks;

namespace TrackYourDay.Tests
{
    [Trait("Category", "Unit")]
    [Trait("Given", "BreakRecordingFeatureIsEnabled")]
    public class BreakRecordingTests
    {
        private Mock<IPublisher> publisherMock;
        private Mock<IClock> clockMock;
        private readonly Features features;

        
        public BreakRecordingTests()
        {
            this.publisherMock = new Mock<IPublisher>();
            this.features = new Features(isBreakRecordingEnabled: true);
            this.clockMock = new Mock<IClock>();
        }

        [Fact]
        public void GivenThereIsNoBreakStarted_WhenThereIsNoActivityInSpecifiedAmountOfTime_ThenBreakIsStarted()
        {
            // Arrange
            var breakTracker = new BreakTracker(this.publisherMock.Object, this.clockMock.Object, this.features.IsBreakRecordingEnabled);
            var lastEventDate = DateTime.Parse("2000-01-01 12:00:00");
            var timeAmountOfNoActivity = TimeSpan.FromMinutes(5);
            breakTracker.AddActivityEventToProcess(ActivityEvent.CreateEvent(lastEventDate, new FocusOnApplication(), "Dummy event"));
            breakTracker.ProcessActivityEvents();

            // Act
            this.clockMock.Setup(x => x.Now).Returns(lastEventDate.Add(timeAmountOfNoActivity));
            breakTracker.ProcessActivityEvents();

            // Assert
            this.publisherMock.Verify(x => x.Publish(It.IsAny<BreakStartedNotifcation>(), CancellationToken.None), Times.Once);
        }

        [Fact]
        public void GivenThereIsStartedBreakBasedOnNoActivity_WhenSystemIsBlocked_ThenBreakIsNotEnded()
        {
            // Arrange
            var breakTracker = new BreakTracker(this.publisherMock.Object, this.clockMock.Object, this.features.IsBreakRecordingEnabled);
            var lastEventDate = DateTime.Parse("2000-01-01 12:00:00");
            var timeAmountOfNoActivity = TimeSpan.FromMinutes(5);
            breakTracker.AddActivityEventToProcess(ActivityEvent.CreateEvent(lastEventDate, new FocusOnApplication(), "Dummy event"));
            breakTracker.ProcessActivityEvents();
            this.clockMock.Setup(x => x.Now).Returns(lastEventDate.Add(timeAmountOfNoActivity));
            breakTracker.ProcessActivityEvents();
            //TODO: Handle above in a better way

            // Act
            breakTracker.AddActivityEventToProcess(ActivityEvent.CreateEvent(lastEventDate, new SystemLocked(), "Dummy event"));

            // Assert
            this.publisherMock.Verify(x => x.Publish(It.IsAny<BreakEndedNotifcation>(), CancellationToken.None), Times.Never);
        }

        [Fact]
        public void GivenThereIsStartedBreakBasedOnNoActivity_WhenThereIsAnyActivityOtherThanSystemLocked_ThenBreakIsEnded()
        {
            // Arrange
            var breakTracker = new BreakTracker(this.publisherMock.Object, this.clockMock.Object, this.features.IsBreakRecordingEnabled);
            var lastEventDate = DateTime.Parse("2000-01-01 12:00:00");
            var timeAmountOfNoActivity = TimeSpan.FromMinutes(5);
            breakTracker.AddActivityEventToProcess(ActivityEvent.CreateEvent(lastEventDate, new FocusOnApplication(), "Dummy event"));
            breakTracker.ProcessActivityEvents();
            this.clockMock.Setup(x => x.Now).Returns(lastEventDate.Add(timeAmountOfNoActivity));
            breakTracker.ProcessActivityEvents();
            //TODO: Handle above in a better way

            // Act
            breakTracker.AddActivityEventToProcess(ActivityEvent.CreateEvent(lastEventDate, new FocusOnApplication(), "Dummy event"));
            breakTracker.ProcessActivityEvents();

            // Assert
            this.publisherMock.Verify(x => x.Publish(It.IsAny<BreakEndedNotifcation>(), CancellationToken.None), Times.Once);
        }

        [Fact]
        public void GivenThereIsNoBreakStarted_WhenUserSessionInOperatingSystemIsBlocked_ThenBreakIsStarted()
        {
            // Arrange
            var breakTracker = new BreakTracker(this.publisherMock.Object, this.clockMock.Object, this.features.IsBreakRecordingEnabled);
            breakTracker.AddActivityEventToProcess(ActivityEvent.CreateEvent(DateTime.Now, new SystemLocked(), "Dummy event"));

            // Act
            breakTracker.ProcessActivityEvents();

            // Assert
            this.publisherMock.Verify(x => x.Publish(It.IsAny<BreakStartedNotifcation>(), CancellationToken.None), Times.Once);
        }

        [Fact]
        public void GivenBreakWasStartedBasedOnSystemLocked_WhenAnyApplicationIsFocused_ThenBreakIsEnded()
        {
            // Arrange
            var breakTracker = new BreakTracker(this.publisherMock.Object, this.clockMock.Object, this.features.IsBreakRecordingEnabled);
            breakTracker.AddActivityEventToProcess(ActivityEvent.CreateEvent(DateTime.Now, new SystemLocked(), "Dummy event"));
            breakTracker.AddActivityEventToProcess(ActivityEvent.CreateEvent(DateTime.Now, new FocusOnApplication(), "Dummy event"));

            // Act
            breakTracker.ProcessActivityEvents();

            // Assert
            this.publisherMock.Verify(x => x.Publish(It.IsAny<BreakEndedNotifcation>(), CancellationToken.None), Times.Once);
        }

        [Fact(Skip = "To be implemented in future")]
        public void GivenWhenBreakRecordingEnds_ThenBreakWaitsForConfirming()
        {
            // Arrange
            var breakTracker = new BreakTracker(this.publisherMock.Object, this.clockMock.Object, this.features.IsBreakRecordingEnabled);
            breakTracker.AddActivityEventToProcess(ActivityEvent.CreateEvent(DateTime.Now, new SystemLocked(), "Dummy event"));
            breakTracker.AddActivityEventToProcess(ActivityEvent.CreateEvent(DateTime.Now, new FocusOnApplication(), "Dummy event"));

            // Act
            breakTracker.ProcessActivityEvents();

            // Assert
            Assert.Fail("Postpone this test and feature. Do always on ending instead.");
            //this.publisherMock.Verify(x => x.Publish(It.IsAny<BreakEndedNotifcation>(), CancellationToken.None), Times.Once);
        }
    }
}