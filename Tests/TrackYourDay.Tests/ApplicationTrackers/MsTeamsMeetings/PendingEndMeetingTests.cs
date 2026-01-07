using FluentAssertions;
using Moq;
using TrackYourDay.Core;
using TrackYourDay.Core.ApplicationTrackers.MsTeams;

namespace TrackYourDay.Tests.ApplicationTrackers.MsTeamsMeetings;

[Trait("Category", "Unit")]
public sealed class PendingEndMeetingTests
{
    [Fact]
    public void GivenPendingEnd_WhenWithinWindow_ThenNotExpired()
    {
        // Given
        var detectedAt = new DateTime(2026, 1, 7, 10, 0, 0);
        var meeting = new StartedMeeting(Guid.NewGuid(), detectedAt.AddMinutes(-30), "Test Meeting");
        var pending = new PendingEndMeeting
        {
            Meeting = meeting,
            DetectedAt = detectedAt,
            ConfirmationWindow = TimeSpan.FromMinutes(2)
        };

        var clockMock = new Mock<IClock>();
        clockMock.Setup(x => x.Now).Returns(detectedAt.AddMinutes(1));

        // When
        var isExpired = pending.IsExpired(clockMock.Object);

        // Then
        isExpired.Should().BeFalse();
    }

    [Fact]
    public void GivenPendingEnd_WhenExactlyAtExpiry_ThenExpired()
    {
        // Given
        var detectedAt = new DateTime(2026, 1, 7, 10, 0, 0);
        var meeting = new StartedMeeting(Guid.NewGuid(), detectedAt.AddMinutes(-30), "Test Meeting");
        var pending = new PendingEndMeeting
        {
            Meeting = meeting,
            DetectedAt = detectedAt,
            ConfirmationWindow = TimeSpan.FromMinutes(2)
        };

        var clockMock = new Mock<IClock>();
        clockMock.Setup(x => x.Now).Returns(detectedAt.AddMinutes(2));

        // When
        var isExpired = pending.IsExpired(clockMock.Object);

        // Then
        isExpired.Should().BeTrue();
    }

    [Fact]
    public void GivenPendingEnd_WhenAfterExpiry_ThenExpired()
    {
        // Given
        var detectedAt = new DateTime(2026, 1, 7, 10, 0, 0);
        var meeting = new StartedMeeting(Guid.NewGuid(), detectedAt.AddMinutes(-30), "Test Meeting");
        var pending = new PendingEndMeeting
        {
            Meeting = meeting,
            DetectedAt = detectedAt,
            ConfirmationWindow = TimeSpan.FromMinutes(2)
        };

        var clockMock = new Mock<IClock>();
        clockMock.Setup(x => x.Now).Returns(detectedAt.AddMinutes(5));

        // When
        var isExpired = pending.IsExpired(clockMock.Object);

        // Then
        isExpired.Should().BeTrue();
    }

    [Fact]
    public void GivenPendingEnd_WhenCreated_ThenHasDefaultConfirmationWindow()
    {
        // Given/When
        var meeting = new StartedMeeting(Guid.NewGuid(), DateTime.Now, "Test Meeting");
        var pending = new PendingEndMeeting
        {
            Meeting = meeting,
            DetectedAt = DateTime.Now
        };

        // Then
        pending.ConfirmationWindow.Should().Be(TimeSpan.FromMinutes(2));
    }
}
