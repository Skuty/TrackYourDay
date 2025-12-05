namespace TrackYourDay.Core.ApplicationTrackers.MsTeams
{
    public record class EndedMeeting (Guid Guid, DateTime StartDate, DateTime EndDate, string Title)
    {
        public string Description { get; set; } = string.Empty;

        public TimeSpan GetDuration()
        {
            return EndDate - StartDate;
        }

        public string GetDescription()
        {
            return !string.IsNullOrWhiteSpace(Description) ? Description : Title;
        }

        public void SetDescription(string description)
        {
            Description = description;
        }
    }
}
