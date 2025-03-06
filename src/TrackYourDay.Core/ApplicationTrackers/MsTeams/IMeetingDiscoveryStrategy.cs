namespace TrackYourDay.Core.ApplicationTrackers.MsTeams
{
    public interface IMeetingDiscoveryStrategy
    {
        StartedMeeting RecognizeMeeting();
    }
}
