namespace TrackYourDay.Core.Activities
{
    public record class FocusOnApplicationActivityType : ActivityType
    {
        public FocusOnApplicationActivityType(string ApplicationWindowTitle) : base($"Focus on application - {ApplicationWindowTitle}")
        {
        }
    }
}