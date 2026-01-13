using FluentAssertions;
using Newtonsoft.Json;
using TrackYourDay.Core.ApplicationTrackers.MsTeams;

namespace TrackYourDay.Tests.ApplicationTrackers.MsTeamsMeetings;

/// <summary>
/// Tests for EndedMeeting serialization, specifically CustomDescription persistence.
/// </summary>
public class EndedMeetingSerializationTests
{
    [Fact]
    public void GivenMeetingWithCustomDescription_WhenSerializedAndDeserialized_ThenCustomDescriptionIsPreserved()
    {
        // Given
        var guid = Guid.NewGuid();
        var startDate = DateTime.Now;
        var endDate = startDate.AddHours(1);
        var meeting = new EndedMeeting(guid, startDate, endDate, "Original Meeting Title");
        meeting.SetCustomDescription("Custom description from user");

        // When
        var json = JsonConvert.SerializeObject(meeting, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto
        });
        var deserialized = JsonConvert.DeserializeObject<EndedMeeting>(json, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto
        });

        // Then
        deserialized.Should().NotBeNull();
        deserialized!.CustomDescription.Should().Be("Custom description from user");
        deserialized.GetDescription().Should().Be("Custom description from user");
        deserialized.Title.Should().Be("Original Meeting Title");
    }

    [Fact]
    public void GivenMeetingWithoutCustomDescription_WhenSerializedAndDeserialized_ThenTitleIsReturned()
    {
        // Given
        var guid = Guid.NewGuid();
        var startDate = DateTime.Now;
        var endDate = startDate.AddHours(1);
        var meeting = new EndedMeeting(guid, startDate, endDate, "Meeting Title Only");

        // When
        var json = JsonConvert.SerializeObject(meeting, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto
        });
        var deserialized = JsonConvert.DeserializeObject<EndedMeeting>(json, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto
        });

        // Then
        deserialized.Should().NotBeNull();
        deserialized!.CustomDescription.Should().BeNullOrWhiteSpace();
        deserialized.GetDescription().Should().Be("Meeting Title Only");
    }

    [Fact]
    public void GivenMeetingWithCustomDescription_WhenDeserializedFromDatabaseJson_ThenCustomDescriptionIsAccessible()
    {
        // Given - Simulating JSON that would be stored in SQLite database
        var guid = Guid.NewGuid();
        var startDate = DateTime.Now;
        var endDate = startDate.AddHours(1);
        
        // This simulates what GenericDataRepository.Save() produces
        var meeting = new EndedMeeting(guid, startDate, endDate, "Database Meeting");
        meeting.SetCustomDescription("Description stored in DB");
        
        var databaseJson = JsonConvert.SerializeObject(meeting, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto
        });

        // When - Simulating what GenericDataRepository.Find() does
        var retrievedMeeting = JsonConvert.DeserializeObject<EndedMeeting>(databaseJson, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto
        });

        // Then
        retrievedMeeting.Should().NotBeNull();
        retrievedMeeting!.HasCustomDescription.Should().BeTrue();
        retrievedMeeting.CustomDescription.Should().Be("Description stored in DB");
        retrievedMeeting.GetDescription().Should().Be("Description stored in DB", 
            "Analytics view should display the custom description, not the meeting title");
    }
}
