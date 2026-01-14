
namespace TrackYourDay.Core.ApplicationTrackers.Jira
{
    public record class JiraSettings
    {
        public required string ApiUrl { get; init; }
        public required string ApiKey { get; init; }
        public bool Enabled { get; init; }
        public int FetchIntervalMinutes { get; init; } = 15;
        public int CircuitBreakerThreshold { get; init; } = 5;
        public int CircuitBreakerDurationMinutes { get; init; } = 5;

        internal static JiraSettings CreateDefaultSettings()
        {
            return new JiraSettings
            {
                ApiUrl = string.Empty,
                ApiKey = string.Empty,
                Enabled = false,
                FetchIntervalMinutes = 15,
                CircuitBreakerThreshold = 5,
                CircuitBreakerDurationMinutes = 5
            };
        }
    }
}