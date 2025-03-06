namespace TrackYourDay.Core.ApplicationTrackers.MsTeams
{
    public record class StartedMeeting (Guid Guid, DateTime StartDate, string Title)
    {
        public EndedMeeting End(DateTime endDate)
        {
            return new EndedMeeting(Guid, StartDate, endDate, Title);
        }

        public TimeSpan GetDuration(IClock clock)
        {
            return clock.Now - StartDate;
        }
    }
}
