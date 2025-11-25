using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using TrackYourDay.Core;
using TrackYourDay.Core.ApplicationTrackers.Breaks;
using TrackYourDay.Core.ApplicationTrackers.Breaks.Events;
using TrackYourDay.Core.SystemTrackers;
using TrackYourDay.Core.SystemTrackers.SystemStates;

namespace TrackYourDay.Tests.ApplicationTrackers.Breaks
{
    [Trait("Category", "Unit")]
    [Trait("Given", "BreakRecordingFeatureIsEnabled")]
    public class BreakTrackingTests
    {
        private Mock<ILogger<BreakTracker>> loggerMock;
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
                            ActivityFactory.MouseMovedActivity(DateTime.Parse("2000-01-01 12:10:00"), new MousePositionState(0,0)) }
            };


        public BreakTrackingTests()
        {
            publisherMock = new Mock<IPublisher>();
            clockMock = new Mock<IClock>();
            timeOfNoActivityToStartBreak = TimeSpan.FromMinutes(5);
            loggerMock = new Mock<ILogger<BreakTracker>>();
        }

        [Fact]
        public void GivenThereIsNoBreakStarted_WhenThereIsNoActivityInSpecifiedAmountOfTime_ThenBreakIsStarted()
        {
            // Arrange
            var breakTracker = new BreakTracker(publisherMock.Object, clockMock.Object, timeOfNoActivityToStartBreak, loggerMock.Object);
            var breakStartDate = DateTime.Parse("2000-01-01 12:00:00");
            clockMock.Setup(x => x.Now).Returns(breakStartDate);
            breakTracker.ProcessActivities();
            clockMock.Setup(x => x.Now).Returns(DateTime.Parse("2000-01-01 12:06:00"));

            // Act
            breakTracker.ProcessActivities();

            // Assert
            publisherMock.Verify(x => x.Publish(It.Is<BreakStartedEvent>(n => n.StartedBreak.BreakStartedAt == breakStartDate), CancellationToken.None), Times.Once);
        }

        /// <summary>
        /// https://github.com/Skuty/TrackYourDay/issues/14#issuecomment-1803276288
        /// Suggest that this scenario does not work while application is running
        /// </summary>
        [Fact]
        public void GivenThereIsNoStartedBreakBut_WhenSystemIsBlocked_ThenBreakIsNotEndedAndBreakIsNotStarted()
        {
            // Arrange
            var breakTracker = new BreakTracker(publisherMock.Object, clockMock.Object, timeOfNoActivityToStartBreak, loggerMock.Object);
            clockMock.Setup(x => x.Now).Returns(DateTime.Parse("2000-01-01 12:00:00"));
            breakTracker.ProcessActivities();
            clockMock.Setup(x => x.Now).Returns(DateTime.Parse("2000-01-01 12:06:00"));
            breakTracker.ProcessActivities();
            publisherMock.Reset();

            // Act
            var startedActivity = ActivityFactory.StartedSystemLockedActivity(DateTime.Parse("2000-01-01 12:08:00"));
            breakTracker.AddActivityToProcess(startedActivity.StartDate, startedActivity.SystemState, Guid.Empty);
            breakTracker.ProcessActivities();

            // Assert
            publisherMock.Verify(x => x.Publish(It.IsAny<BreakEndedEvent>(), CancellationToken.None), Times.Never);
            publisherMock.Verify(x => x.Publish(It.IsAny<BreakStartedEvent>(), CancellationToken.None), Times.Never);
        }


        [Fact]
        public void GivenThereIsStartedBreak_WhenSystemIsBlocked_ThenBreakIsNotEndedAndBreakIsNotStarted()
        {
            // Arrange
            var breakTracker = new BreakTracker(publisherMock.Object, clockMock.Object, timeOfNoActivityToStartBreak, loggerMock.Object);
            clockMock.Setup(x => x.Now).Returns(DateTime.Parse("2000-01-01 12:00:00"));
            breakTracker.ProcessActivities();
            clockMock.Setup(x => x.Now).Returns(DateTime.Parse("2000-01-01 12:06:00"));
            breakTracker.ProcessActivities();
            publisherMock.Reset();

            // Act
            var startedActivity = ActivityFactory.StartedSystemLockedActivity(DateTime.Now);
            breakTracker.AddActivityToProcess(startedActivity.StartDate, startedActivity.SystemState, Guid.Empty);
            breakTracker.ProcessActivities();

            // Assert
            publisherMock.Verify(x => x.Publish(It.IsAny<BreakEndedEvent>(), CancellationToken.None), Times.Never);
            publisherMock.Verify(x => x.Publish(It.IsAny<BreakStartedEvent>(), CancellationToken.None), Times.Never);
        }

        [Fact]
        public void GivenThereIsNoBreakStarted_WhenUserSessionInOperatingSystemIsBlocked_ThenBreakIsStarted()
        {
            // Arrange
            var breakTracker = new BreakTracker(publisherMock.Object, clockMock.Object, timeOfNoActivityToStartBreak, loggerMock.Object);
            var breakStartDate = DateTime.Parse("2000-01-01 12:00:00");
            var activityToProcess = ActivityFactory.StartedSystemLockedActivity(breakStartDate);
            breakTracker.AddActivityToProcess(activityToProcess.StartDate, activityToProcess.SystemState, Guid.Empty);

            // Act
            breakTracker.ProcessActivities();

            // Assert
            publisherMock.Verify(x => x.Publish(It.Is<BreakStartedEvent>(n => n.StartedBreak.BreakStartedAt == breakStartDate), CancellationToken.None), Times.Once);
        }

        [Theory]
        [MemberData(nameof(PeriodicActivitiesToStopBreak))]
        public void GivenThereIsStartedBreak_WhenThereIsAnyPeriodicActivityOtherThanSystemLocked_ThenBreakIsEnded(StartedActivity startedActivity)
        {
            // Arrange
            var breakTracker = new BreakTracker(publisherMock.Object, clockMock.Object, timeOfNoActivityToStartBreak, loggerMock.Object);
            var breakStartedDate = DateTime.Parse("2000-01-01 12:00:00");
            clockMock.Setup(x => x.Now).Returns(breakStartedDate);
            breakTracker.ProcessActivities();
            clockMock.Setup(x => x.Now).Returns(DateTime.Parse("2000-01-01 12:06:00"));
            breakTracker.ProcessActivities();

            // Act
            breakTracker.AddActivityToProcess(startedActivity.StartDate, startedActivity.SystemState, Guid.Empty);
            breakTracker.ProcessActivities();

            // Assert
            publisherMock.Verify(x => x.Publish(It.Is<BreakEndedEvent>(
                n => n.EndedBreak.BreakEndedAt == startedActivity.StartDate
                && n.EndedBreak.BreakStartedAt == breakStartedDate
                ), CancellationToken.None), Times.Once);
        }

        [Fact]
        public void GivenThereIsStartedBreak_WhenThereIsAnyInstantActivity_ThenBreakIsEnded()
        {
            // Arrange
            var breakTracker = new BreakTracker(publisherMock.Object, clockMock.Object, timeOfNoActivityToStartBreak, loggerMock.Object);
            var breakStartDate = DateTime.Parse("2000-01-01 12:00:00");
            clockMock.Setup(x => x.Now).Returns(breakStartDate);
            breakTracker.ProcessActivities();
            clockMock.Setup(x => x.Now).Returns(DateTime.Parse("2000-01-01 12:06:00"));
            breakTracker.ProcessActivities();
            var instantActivity = ActivityFactory.MouseMovedActivity(DateTime.Parse("2000-01-01 12:10:00"), new MousePositionState(0, 0));

            // Act
            breakTracker.AddActivityToProcess(instantActivity.OccuranceDate, instantActivity.SystemState, Guid.Empty); 
            
            breakTracker.ProcessActivities();

            // Assert
            publisherMock.Verify(x => x.Publish(It.Is<BreakEndedEvent>(
                n => n.EndedBreak.BreakEndedAt == instantActivity.OccuranceDate
                && n.EndedBreak.BreakStartedAt == breakStartDate
                ), CancellationToken.None), Times.Once);
        }

        [Fact(Skip ="Should be enabled when Pending Break concept will be introduced")]
        // TODO:Consider PendingBreak concept which will represent potential break which didn't meet minimal requirements
        // In example system locked for less than 3 minutes
        public void GivenThereIsStartedBreakAndSystemIsLocked_WhenThereIsAnyInstantActivity_ThenBreakIsNotEnded()
        {
            // Arrange
            var breakTracker = new BreakTracker(publisherMock.Object, clockMock.Object, timeOfNoActivityToStartBreak, loggerMock.Object);
            var breakStartDate = DateTime.Parse("2000-01-01 12:00:00");
            clockMock.Setup(x => x.Now).Returns(breakStartDate);
            breakTracker.ProcessActivities();
            clockMock.Setup(x => x.Now).Returns(DateTime.Parse("2000-01-01 12:06:00"));
            breakTracker.ProcessActivities();
            var instantActivity = ActivityFactory.MouseMovedActivity(DateTime.Parse("2000-01-01 12:10:00"), new MousePositionState(0, 0));

            // Act
            breakTracker.AddActivityToProcess(instantActivity.OccuranceDate, instantActivity.SystemState, Guid.Empty);
            breakTracker.ProcessActivities();
            // TODO: Fix implementation as it is not working as expected

            // Assert
            publisherMock.Verify(x => x.Publish(It.Is<BreakEndedEvent>(
                n => n.EndedBreak.BreakEndedAt == instantActivity.OccuranceDate
                && n.EndedBreak.BreakStartedAt == breakStartDate
                ), CancellationToken.None), Times.Never);
        }


        [Fact(Skip = "Postponed")]
        public void Error_BreakStartDateWasNewRecognizedActivityInsteadOfLastActivityDate()
        {
        }

        [Fact]
        public void GivenThereIsEndedBreak_WhenBreakIsRevoked_ThenBreakIsRevoked()
        {
            // TODO: Resolve BreakTracker responsibilties to allow easier testing
            // Probably changing BreakTracker to operate on Workday may be a solution
            // Arrange
            var breakTracker = new BreakTracker(publisherMock.Object, clockMock.Object, timeOfNoActivityToStartBreak, loggerMock.Object);
            var breakStartDate = DateTime.Parse("2000-01-01 12:00:00");
            clockMock.Setup(x => x.Now).Returns(breakStartDate);
            breakTracker.ProcessActivities();
            clockMock.Setup(x => x.Now).Returns(DateTime.Parse("2000-01-01 12:06:00"));
            breakTracker.ProcessActivities();
            var instantActivity = ActivityFactory.MouseMovedActivity(DateTime.Parse("2000-01-01 12:10:00"), new MousePositionState(0, 0));
            breakTracker.AddActivityToProcess(instantActivity.OccuranceDate, instantActivity.SystemState, Guid.Empty);
            breakTracker.ProcessActivities();

            // TODO: Remove any callbacks to this remporary GetEndedBreaks method
            var endedBreak = breakTracker.GetEndedBreaks().First();

            // Act
            breakTracker.RevokeBreak(endedBreak.Guid, DateTime.Now);

            // Assert
            publisherMock.Verify(x => x.Publish(It.Is<BreakRevokedEvent>(
                n => n.RevokedBreak.BreakGuid == endedBreak.Guid
                ), CancellationToken.None), Times.Once);
        }
    }
}