using TrackYourDay.Core.Insights.Analytics;

namespace TrackYourDay.Core.ApplicationTrackers.MsTeams
{
    public record class EndedMeeting (Guid Guid, DateTime StartDate, DateTime EndDate, string Title) : ITrackableItem
    {
        public string Description { get; private set; } = string.Empty;

        public TimeSpan GetDuration()
        {
            return EndDate - StartDate;
        }

        public string GetDescription()
        {
            return !string.IsNullOrWhiteSpace(Description) ? Description : Title;
        }

        public void Describe(string description)
        {
            Description = description;
        }
    }
}
