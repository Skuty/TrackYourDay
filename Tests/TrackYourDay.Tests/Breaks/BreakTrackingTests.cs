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
        private Mock<IPublisher> publisherMock;
        private Mock<IClock> clockMock;
        private TimeSpan timeOfNoActivityToStartBreak;
        private BreakTracker breakTracker;

        public static IEnumerable<object[]> PeriodicActivitiesToStopBreak =>
            new List<object[]>
            {
                new object[] { 
                    ActivityFactory.StartedFocusOnApplicatoinActivity(DateTime.Parse("2000-01-01 12:00:00")) }
            };

        public static IEnumerable<object[]> InstantActivitiesToStopBreak =>
            new List<object[]>
            {
                        new object[] {
                            ActivityFactory.MouseMovedActivity(DateTime.Parse("2000-01-01 12:00:00")) }
            };


        public BreakTrackingTests()
        {
            this.publisherMock = new Mock<IPublisher>();
            this.clockMock = new Mock<IClock>();
            this.timeOfNoActivityToStartBreak = TimeSpan.FromMinutes(5);
        }

        [Fact]
        public void GivenThereIsNoBreakStarted_WhenThereIsNoActivityInSpecifiedAmountOfTime_ThenBreakIsStarted()
        {
            // Arrange
            var breakTracker = new BreakTracker(publisherMock.Object, clockMock.Object, this.timeOfNoActivityToStartBreak);
            this.clockMock.Setup(x => x.Now).Returns(DateTime.Parse("2000-01-01 12:00:00"));
            breakTracker.ProcessActivities();
            this.clockMock.Setup(x => x.Now).Returns(DateTime.Parse("2000-01-01 12:06:00"));

            // Act
            breakTracker.ProcessActivities();

            // Assert
            this.publisherMock.Verify(x => x.Publish(It.IsAny<BreakStartedNotifcation>(), CancellationToken.None), Times.Once);
        }

        [Fact]
        public void GivenThereIsStartedBreak_WhenSystemIsBlocked_ThenBreakIsNotEndedAndBreakIsNotStarted()
        {
            // Arrange
            var breakTracker = new BreakTracker(publisherMock.Object, clockMock.Object, this.timeOfNoActivityToStartBreak);
            this.clockMock.Setup(x => x.Now).Returns(DateTime.Parse("2000-01-01 12:00:00"));
            breakTracker.ProcessActivities();
            this.clockMock.Setup(x => x.Now).Returns(DateTime.Parse("2000-01-01 12:06:00"));
            breakTracker.ProcessActivities();
            this.publisherMock.Reset();

            // Act
            var startedActivity = ActivityFactory.StartedSystemLockedActivity(DateTime.Now);
            breakTracker.AddActivityToProcess(startedActivity.StartDate, startedActivity.ActivityType);
            breakTracker.ProcessActivities();

            // Assert
            publisherMock.Verify(x => x.Publish(It.IsAny<BreakEndedNotifcation>(), CancellationToken.None), Times.Never);
            publisherMock.Verify(x => x.Publish(It.IsAny<BreakStartedNotifcation>(), CancellationToken.None), Times.Never);
        }

        [Fact]
        public void GivenThereIsNoBreakStarted_WhenUserSessionInOperatingSystemIsBlocked_ThenBreakIsStarted()
        {
            // Arrange
            var breakTracker = new BreakTracker(publisherMock.Object, clockMock.Object, this.timeOfNoActivityToStartBreak);
            var activityToProcess = ActivityFactory.StartedSystemLockedActivity(DateTime.Now);
            breakTracker.AddActivityToProcess(activityToProcess.StartDate, activityToProcess.ActivityType);

            // Act
            breakTracker.ProcessActivities();

            // Assert
            publisherMock.Verify(x => x.Publish(It.IsAny<BreakStartedNotifcation>(), CancellationToken.None), Times.Once);
        }

        [Theory]
        [MemberData(nameof(PeriodicActivitiesToStopBreak))]
        public void GivenThereIsStartedBreak_WhenThereIsAnyPeriodicActivityOtherThanSystemLocked_ThenBreakIsEnded(StartedActivity startedActivity)
        {
            // Arrange
            var breakTracker = new BreakTracker(publisherMock.Object, clockMock.Object, this.timeOfNoActivityToStartBreak);
            this.clockMock.Setup(x => x.Now).Returns(DateTime.Parse("2000-01-01 12:00:00"));
            breakTracker.ProcessActivities();
            this.clockMock.Setup(x => x.Now).Returns(DateTime.Parse("2000-01-01 12:06:00"));
            breakTracker.ProcessActivities();

            // Act
            breakTracker.AddActivityToProcess(startedActivity.StartDate, startedActivity.ActivityType);
            breakTracker.ProcessActivities();

            // Assert
            publisherMock.Verify(x => x.Publish(It.IsAny<BreakEndedNotifcation>(), CancellationToken.None), Times.Once);
        }

        [Theory]
        [MemberData(nameof(InstantActivitiesToStopBreak))]
        public void GivenThereIsStartedBreak_WhenThereIsAnyInstantActivityOtherThanSystemLocked_ThenBreakIsEnded(InstantActivity instantActivity)
        {
            // Arrange
            var breakTracker = new BreakTracker(publisherMock.Object, clockMock.Object, this.timeOfNoActivityToStartBreak);
            this.clockMock.Setup(x => x.Now).Returns(DateTime.Parse("2000-01-01 12:00:00"));
            breakTracker.ProcessActivities();
            this.clockMock.Setup(x => x.Now).Returns(DateTime.Parse("2000-01-01 12:06:00"));
            breakTracker.ProcessActivities();

            // Act
            breakTracker.AddActivityToProcess(instantActivity.OccuranceDate, instantActivity.ActivityType); 
            
            breakTracker.ProcessActivities();

            // Assert
            publisherMock.Verify(x => x.Publish(It.IsAny<BreakEndedNotifcation>(), CancellationToken.None), Times.Once);
        }

        [Fact(Skip = "To be implemented in future")]
        public void GivenWhenBreakRecordingEnds_ThenBreakWaitsForConfirming()
        {
            // Arrange
            //var breakTracker = new BreakTracker(publisherMock.Object, clockMock.Object);
            //breakTracker.AddActivityToProcess(ActivityEvent.CreateEvent(DateTime.Now, new SystemLockedActivity()));
            //breakTracker.AddActivityToProcess(ActivityEvent.CreateEvent(DateTime.Now, new FocusOnApplicationActivity(string.Empty)));

            //// Act
            //breakTracker.ProcessActivities();

            //// Assert
            //Assert.Fail("Postpone this test and feature. Do always on ending instead.");
            //this.publisherMock.Verify(x => x.Publish(It.IsAny<BreakEndedNotifcation>(), CancellationToken.None), Times.Once);
        }
    }
}