namespace TrackYourDay.Core.Old.Activities
{
    public abstract record class Activity(string Name);

    public record class SystemLockedActivity() : Activity("System Locked")
    {
        public override string ToString()
        {
            return Name;
        }
    }

    public record class FocusOnApplicationActivity(string ApplicationName) : Activity("Focus on Application")
    {
        public override string ToString()
        {
            return Name + " " + ApplicationName;
        }
    }
}
