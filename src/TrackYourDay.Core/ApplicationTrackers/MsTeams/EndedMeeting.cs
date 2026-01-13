using Newtonsoft.Json;
using TrackYourDay.Core.Insights.Analytics;

namespace TrackYourDay.Core.ApplicationTrackers.MsTeams
{
    /// <summary>
    /// Represents a completed MS Teams meeting.
    /// </summary>
    public sealed class EndedMeeting : TrackedActivity
    {
        public string Title { get; init; }
        
        /// <summary>
        /// Custom description that overrides the meeting title.
        /// JsonProperty attribute ensures serialization/deserialization works with private setter.
        /// </summary>
        [JsonProperty]
        public string? CustomDescription { get; private set; }
        
        public EndedMeeting(Guid guid, DateTime startDate, DateTime endDate, string title)
            : base(guid, startDate, endDate)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Meeting title cannot be empty", nameof(title));
            
            Title = title;
        }
        
        public override string GetDescription()
        {
            return !string.IsNullOrWhiteSpace(CustomDescription) ? CustomDescription : Title;
        }
        
        /// <summary>
        /// Sets a custom description that overrides the meeting title.
        /// </summary>
        public void SetCustomDescription(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Description cannot be empty", nameof(description));
            
            CustomDescription = description;
        }
        
        /// <summary>
        /// Checks if the meeting has been customized with a description.
        /// </summary>
        public bool HasCustomDescription => !string.IsNullOrWhiteSpace(CustomDescription);
    }
}
