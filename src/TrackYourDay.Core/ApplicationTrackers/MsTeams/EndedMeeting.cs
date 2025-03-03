namespace TrackYourDay.Core.ApplicationTrackers.MsTeams
{
    public record class EndedMeeting (Guid Guid, DateTime StartDate, DateTime EndDate, string Title)
    {
        public TimeSpan GetDuration()
        {
            return EndDate - StartDate;
        }

        public string GetDescription()
        {
            return Title;
        }

    }
}
