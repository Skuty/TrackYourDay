using MediatR;
using Moq;
using TrackYourDay.Core;
using TrackYourDay.Core.Activities;
using TrackYourDay.Core.Breaks;
using TrackYourDay.Core.Breaks.Notifications;

namespace TrackYourDay.Tests.Breaks
{
    [Trait("Category", "Unit")]
    [Trait("Given", "BreakRecordingFeatureIsEnabled")]
    public class BreakTrackingTests
    {
        public static IEnumerable<object[]> ActivityEventsToStopBreak =>
            new List<object[]>
            {
                new object[] { ActivityEvent.CreateEvent(DateTime.Parse("2000-01-01 12:00"), new FocusOnApplicationActivity()) }
            };

        private Mock<IPublisher> publisherMock;
        private Mock<IClock> clockMock;
        private readonly Features features;

        public BreakTrackingTests()
        {
            publisherMock = new Mock<IPublisher>();
            features = new Features(isBreakRecordingEnabled: true);
            clockMock = new Mock<IClock>();
        }

        [Fact]
        public void GivenThereIsNoBreakStarted_WhenThereIsNoActivityInSpecifiedAmountOfTime_ThenBreakIsStarted()
        {
            // Arrange
            var breakTracker = new BreakTracker(publisherMock.Object, clockMock.Object, features.IsBreakRecordingEnabled);
            var lastEventDate = DateTime.Parse("2000-01-01 12:00:00");
            var timeAmountOfNoActivity = TimeSpan.FromMinutes(5);
            breakTracker.AddActivityEventToProcess(ActivityEvent.CreateEvent(lastEventDate, new FocusOnApplicationActivity()));
            breakTracker.ProcessActivityEvents();

            // Act
            clockMock.Setup(x => x.Now).Returns(lastEventDate.Add(timeAmountOfNoActivity));
            breakTracker.ProcessActivityEvents();

            // Assert
            publisherMock.Verify(x => x.Publish(It.IsAny<BreakStartedNotifcation>(), CancellationToken.None), Times.Once);
        }

        [Fact]
        public void GivenThereIsStartedBreak_WhenSystemIsBlocked_ThenBreakIsNotEnded()
        {
            // Arrange
            var startedBreak = new StartedBreak(DateTime.Parse("2000-01-01 00:00"));
            var breakTracker = new BreakTracker(startedBreak, publisherMock.Object, clockMock.Object, features.IsBreakRecordingEnabled);

            // Act
            breakTracker.AddActivityEventToProcess(ActivityEvent.CreateEvent(DateTime.Parse("2000-01-01 01:00"), new SystemLockedActivity()));

            // Assert
            publisherMock.Verify(x => x.Publish(It.IsAny<BreakEndedNotifcation>(), CancellationToken.None), Times.Never);
        }

        [Fact]
        public void GivenThereIsNoBreakStarted_WhenUserSessionInOperatingSystemIsBlocked_ThenBreakIsStarted()
        {
            // Arrange
            var breakTracker = new BreakTracker(publisherMock.Object, clockMock.Object, features.IsBreakRecordingEnabled);
            breakTracker.AddActivityEventToProcess(ActivityEvent.CreateEvent(DateTime.Now, new SystemLockedActivity()));

            // Act
            breakTracker.ProcessActivityEvents();

            // Assert
            publisherMock.Verify(x => x.Publish(It.IsAny<BreakStartedNotifcation>(), CancellationToken.None), Times.Once);
        }

        [Theory]
        [MemberData(nameof(ActivityEventsToStopBreak))]
        public void GivenThereIsStartedBreak_WhenThereIsAnyActivityOtherThanSystemLocked_ThenBreakIsEnded(ActivityEvent activityEvent)
        {
            // Arrange
            var startedBreak = new StartedBreak(DateTime.Parse("2000-01-01 00:00"));
            var breakTracker = new BreakTracker(startedBreak, publisherMock.Object, clockMock.Object, features.IsBreakRecordingEnabled);

            // Act
            breakTracker.AddActivityEventToProcess(activityEvent);
            breakTracker.ProcessActivityEvents();

            // Assert
            publisherMock.Verify(x => x.Publish(It.IsAny<BreakEndedNotifcation>(), CancellationToken.None), Times.Once);
        }

        [Fact(Skip = "To be implemented in future")]
        public void GivenWhenBreakRecordingEnds_ThenBreakWaitsForConfirming()
        {
            // Arrange
            var breakTracker = new BreakTracker(publisherMock.Object, clockMock.Object, features.IsBreakRecordingEnabled);
            breakTracker.AddActivityEventToProcess(ActivityEvent.CreateEvent(DateTime.Now, new SystemLockedActivity()));
            breakTracker.AddActivityEventToProcess(ActivityEvent.CreateEvent(DateTime.Now, new FocusOnApplicationActivity()));

            // Act
            breakTracker.ProcessActivityEvents();

            // Assert
            Assert.Fail("Postpone this test and feature. Do always on ending instead.");
            //this.publisherMock.Verify(x => x.Publish(It.IsAny<BreakEndedNotifcation>(), CancellationToken.None), Times.Once);
        }
    }
}