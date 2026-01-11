using FluentAssertions;
using TrackYourDay.Core.ApplicationTrackers.MsTeams;

namespace TrackYourDay.Tests.ApplicationTrackers.MsTeamsMeetings;

[Trait("Category", "Unit")]
public sealed class PendingEndMeetingTests
{
    [Fact]
    public void GivenPendingEndMeeting_WhenCreated_ThenHasRequiredProperties()
    {
        // Given
        var detectedAt = new DateTime(2026, 1, 7, 10, 0, 0);
        var meeting = new StartedMeeting(Guid.NewGuid(), detectedAt.AddMinutes(-30), "Test Meeting");
        
        // When
        var pending = new PendingEndMeeting
        {
            Meeting = meeting,
            DetectedAt = detectedAt
        };

        // Then
        pending.Meeting.Should().Be(meeting);
        pending.DetectedAt.Should().Be(detectedAt);
    }

    [Fact]
    public void GivenPendingEndMeeting_WhenAccessingMeetingProperties_ThenReturnsCorrectValues()
    {
        // Given
        var meetingGuid = Guid.NewGuid();
        var startDate = new DateTime(2026, 1, 7, 9, 30, 0);
        var detectedAt = new DateTime(2026, 1, 7, 10, 0, 0);
        var meeting = new StartedMeeting(meetingGuid, startDate, "Sprint Planning");
        
        // When
        var pending = new PendingEndMeeting
        {
            Meeting = meeting,
            DetectedAt = detectedAt
        };

        // Then
        pending.Meeting.Guid.Should().Be(meetingGuid);
        pending.Meeting.Title.Should().Be("Sprint Planning");
        pending.Meeting.StartDate.Should().Be(startDate);
    }
}
