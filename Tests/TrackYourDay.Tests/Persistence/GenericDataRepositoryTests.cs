using Moq;
using Newtonsoft.Json;
using TrackYourDay.Core;
using TrackYourDay.Core.ApplicationTrackers.MsTeams;
using TrackYourDay.Core.Persistence;
using TrackYourDay.Core.Persistence.Specifications;

namespace TrackYourDay.Tests.Persistence
{
    [Trait("Category", "Unit")]
    public class GenericDataRepositoryTests : IDisposable
    {
        private readonly Mock<IClock> clockMock;
        private readonly GenericDataRepository<EndedMeeting> repository;
        private readonly string testDbPath;

        public GenericDataRepositoryTests()
        {
            clockMock = new Mock<IClock>();
            clockMock.Setup(c => c.Now).Returns(DateTime.Now);
            
            // Use a test-specific database path
            testDbPath = Path.Combine(Path.GetTempPath(), $"TrackYourDay_Test_{Guid.NewGuid()}.db");
            
            repository = new GenericDataRepository<EndedMeeting>(clockMock.Object);
        }

        public void Dispose()
        {
            repository.Clear();
        }

        [Fact]
        public void EndedMeeting_WhenSerializedAndDeserialized_DescriptionIsPreserved()
        {
            // Given
            var meeting = new EndedMeeting(Guid.NewGuid(), DateTime.Now.AddHours(-1), DateTime.Now, "Test");
            meeting.SetDescription("My Description");

            // When
            var json = JsonConvert.SerializeObject(meeting, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });
            var deserialized = JsonConvert.DeserializeObject<EndedMeeting>(json, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

            // Then
            Assert.NotNull(deserialized);
            Assert.Equal("My Description", deserialized.Description);
        }

        [Fact]
        public void GivenMeetingIsSaved_WhenMeetingIsUpdated_ThenUpdatedMeetingIsPersisted()
        {
            // Given
            var meetingGuid = Guid.NewGuid();
            var startDate = DateTime.Now.AddHours(-1);
            var endDate = DateTime.Now;
            var endedMeeting = new EndedMeeting(meetingGuid, startDate, endDate, "Test Meeting");
            repository.Save(endedMeeting);

            // When
            endedMeeting.SetDescription("Updated description");
            repository.Update(endedMeeting);

            // Then
            var specification = new MeetingByDateSpecification(DateOnly.FromDateTime(startDate));
            var meetings = repository.Find(specification);
            var updatedMeeting = meetings.FirstOrDefault(m => m.Guid == meetingGuid);
            
            Assert.NotNull(updatedMeeting);
            Assert.Equal("Updated description", updatedMeeting.Description);
        }

        [Fact]
        public void GivenMeetingIsSavedWithoutDescription_WhenMeetingIsUpdatedWithDescription_ThenDescriptionIsVisible()
        {
            // Given
            var meetingGuid = Guid.NewGuid();
            var startDate = DateTime.Now.AddHours(-1);
            var endDate = DateTime.Now;
            var endedMeeting = new EndedMeeting(meetingGuid, startDate, endDate, "Project Sync");
            repository.Save(endedMeeting);

            // Initially, description should be empty
            var initialMeetings = repository.Find(new MeetingByDateSpecification(DateOnly.FromDateTime(startDate)));
            var initialMeeting = initialMeetings.FirstOrDefault(m => m.Guid == meetingGuid);
            Assert.NotNull(initialMeeting);
            Assert.Equal(string.Empty, initialMeeting.Description);

            // When
            endedMeeting.SetDescription("Discussed project timeline and milestones");
            repository.Update(endedMeeting);

            // Then
            var updatedMeetings = repository.Find(new MeetingByDateSpecification(DateOnly.FromDateTime(startDate)));
            var updatedMeeting = updatedMeetings.FirstOrDefault(m => m.Guid == meetingGuid);
            
            Assert.NotNull(updatedMeeting);
            Assert.Equal("Discussed project timeline and milestones", updatedMeeting.Description);
            Assert.Equal("Discussed project timeline and milestones", updatedMeeting.GetDescription());
        }
    }
}
