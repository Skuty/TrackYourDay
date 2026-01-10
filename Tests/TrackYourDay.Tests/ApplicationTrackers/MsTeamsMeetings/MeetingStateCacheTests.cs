using FluentAssertions;
using TrackYourDay.Core;
using TrackYourDay.Core.ApplicationTrackers.MsTeams;
using TrackYourDay.Core.ApplicationTrackers.MsTeams.State;

namespace TrackYourDay.Tests.ApplicationTrackers.MsTeamsMeetings;

[Trait("Category", "Unit")]
public class MeetingStateCacheTests
{
    private readonly MeetingStateCache _cache;
    private readonly IClock _clock;

    public MeetingStateCacheTests()
    {
        _cache = new MeetingStateCache();
        _clock = new Clock();
    }

    [Fact]
    public void WhenGetOngoingMeeting_ThenReturnsNullInitially()
    {
        // When
        var meeting = _cache.GetOngoingMeeting();

        // Then
        meeting.Should().BeNull();
    }

    [Fact]
    public void GivenMeetingSet_WhenGetOngoingMeeting_ThenReturnsMeeting()
    {
        // Given
        var meeting = new StartedMeeting(Guid.NewGuid(), _clock.Now, "Test meeting");
        _cache.SetOngoingMeeting(meeting);

        // When
        var retrieved = _cache.GetOngoingMeeting();

        // Then
        retrieved.Should().Be(meeting);
    }

    [Fact]
    public void GivenMeetingSet_WhenSetNull_ThenReturnsNull()
    {
        // Given
        var meeting = new StartedMeeting(Guid.NewGuid(), _clock.Now, "Test meeting");
        _cache.SetOngoingMeeting(meeting);

        // When
        _cache.SetOngoingMeeting(null);
        var retrieved = _cache.GetOngoingMeeting();

        // Then
        retrieved.Should().BeNull();
    }

    [Fact]
    public void WhenGetMatchedRuleId_ThenReturnsNullInitially()
    {
        // When
        var ruleId = _cache.GetMatchedRuleId();

        // Then
        ruleId.Should().BeNull();
    }

    [Fact]
    public void GivenRuleIdSet_WhenGetMatchedRuleId_ThenReturnsRuleId()
    {
        // Given
        var ruleId = Guid.NewGuid();
        _cache.SetMatchedRuleId(ruleId);

        // When
        var retrieved = _cache.GetMatchedRuleId();

        // Then
        retrieved.Should().Be(ruleId);
    }

    [Fact]
    public void GivenMeetingAndRuleIdSet_WhenClearMeetingState_ThenBothAreCleared()
    {
        // Given
        var meeting = new StartedMeeting(Guid.NewGuid(), _clock.Now, "Test meeting");
        var ruleId = Guid.NewGuid();
        _cache.SetOngoingMeeting(meeting);
        _cache.SetMatchedRuleId(ruleId);

        // When
        _cache.ClearMeetingState();

        // Then
        _cache.GetOngoingMeeting().Should().BeNull();
        _cache.GetMatchedRuleId().Should().BeNull();
    }

    [Fact]
    public async Task GivenMultipleThreadsAccess_WhenConcurrentOperations_ThenStateIsConsistent()
    {
        // Given
        var meeting1 = new StartedMeeting(Guid.NewGuid(), _clock.Now, "Meeting 1");
        var meeting2 = new StartedMeeting(Guid.NewGuid(), _clock.Now, "Meeting 2");
        var exceptions = new List<Exception>();

        // When
        var tasks = new List<Task>();
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    _cache.SetOngoingMeeting(meeting1);
                    var m = _cache.GetOngoingMeeting();
                    _cache.SetOngoingMeeting(meeting2);
                    _cache.ClearMeetingState();
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            }));
        }
        await Task.WhenAll(tasks);

        // Then
        exceptions.Should().BeEmpty();
        _cache.GetOngoingMeeting().Should().BeNull();
    }
}
