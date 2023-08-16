namespace TrackYourDay.Core.Activities
{
    public record class ApplicationStartedActivityType : ActivityType
    {
        public ApplicationStartedActivityType(string ApplicationName) : base($"Application started - {ApplicationName}")
        {
        }
    }
}