using MediatR;
using Moq;
using TrackYourDay.Core;
using TrackYourDay.Core.Activities;
using TrackYourDay.Core.Tasks;

namespace TrackYourDay.Tests
{
    public class BreakRecordingTests
    {
        private Mock<IPublisher> publisherMock;
        private readonly Features features;

        public BreakRecordingTests()
        {
            this.publisherMock = new Mock<IPublisher>();
            this.features = new Features(isBreakRecordingEnabled: true);
        }

        [Fact]
        public void GivenBreakRecordingFeatureIsEnabledAndThereIsNoBreakStarted_WhenThereIsNoActivityInSpecifiedAmountOfTime_ThenBreakIsStarted()
        {
            // Arrange
            var clock = new FakeClock();
            var breakTracker = new BreakTracker(this.publisherMock.Object, this.features.IsBreakRecordingEnabled);
            breakTracker.AddEventToProcess(ActivityEvent.CreateEvent(DateTime.Now, new FocusOnApplication(), "Dummy event"));
            breakTracker.ProcessEvents();

            // Act
            clock.SetDate(DateTime.Now.AddMinutes(10));
            breakTracker.ProcessEvents();

            // Assert
            this.publisherMock.Verify(x => x.Publish(It.IsAny<BreakStartedNotifcation>(), CancellationToken.None), Times.Once);
        }

        [Fact]
        public void GivenBreakRecordingFeatureIsEnabledAndThereIsStartedBreakBasedOnNoActivity_WhenThereIsAnyActivityOtherThanSystemLocked_ThenBreakIsEnded()
        {
            Assert.Fail("Feature not implemented");
        }

        [Fact]
        public void GivenBreakRecordingFeatureIsEnabledAndThereIsStartedBreakBasedOnNoActivity_WhenSystemIsBlocked_ThenBreakIsNotEnded()
        {
            Assert.Fail("Feature not implemented");
        }

        [Fact]
        public void GivenBreakRecordingFeatureIsEnabledAndThereIsNoBreakStarted_WhenUserSessionInOperatingSystemIsBlocked_ThenBreakIsStarted()
        {
            // Arrange
            var breakTracker = new BreakTracker(this.publisherMock.Object, this.features.IsBreakRecordingEnabled);
            breakTracker.AddEventToProcess(ActivityEvent.CreateEvent(DateTime.Now, new SystemLocked(), "Dummy event"));

            // Act
            breakTracker.ProcessEvents();

            // Assert
            this.publisherMock.Verify(x => x.Publish(It.IsAny<BreakStartedNotifcation>(), CancellationToken.None), Times.Once);
        }

        [Fact]
        public void GivenBreakRecordingFeatureIsEnabledAndBreakWasStartedBasedOnSystemLocked_WhenAnyApplicationIsFocused_ThenBreakIsEnded()
        {
            // Arrange
            var breakTracker = new BreakTracker(this.publisherMock.Object, this.features.IsBreakRecordingEnabled);
            breakTracker.AddEventToProcess(ActivityEvent.CreateEvent(DateTime.Now, new SystemLocked(), "Dummy event"));
            breakTracker.AddEventToProcess(ActivityEvent.CreateEvent(DateTime.Now, new FocusOnApplication(), "Dummy event"));

            // Act
            breakTracker.ProcessEvents();

            // Assert
            this.publisherMock.Verify(x => x.Publish(It.IsAny<BreakEndedNotifcation>(), CancellationToken.None), Times.Once);
        }

        public void GivenBreakRecordingFeatureIsEnabled_WhenBreakRecordingEnds_ThenBreakWaitsForConfirming()
        {
            // Arrange
            var breakTracker = new BreakTracker(this.publisherMock.Object, this.features.IsBreakRecordingEnabled);
            breakTracker.AddEventToProcess(ActivityEvent.CreateEvent(DateTime.Now, new SystemLocked(), "Dummy event"));
            breakTracker.AddEventToProcess(ActivityEvent.CreateEvent(DateTime.Now, new FocusOnApplication(), "Dummy event"));

            // Act
            breakTracker.ProcessEvents();

            // Assert
            Assert.Fail("Postpone this test and feature. Do always on ending instead.");
            //this.publisherMock.Verify(x => x.Publish(It.IsAny<BreakEndedNotifcation>(), CancellationToken.None), Times.Once);
        }
    }
}