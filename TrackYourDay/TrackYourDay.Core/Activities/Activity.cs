namespace TrackYourDay.Core.Activities
{
    public abstract record class Activity(string Name);

    public record class SystemLockedActivity() : Activity("System Locked");

    public record class FocusOnApplicationActivity() : Activity("Focus on Application");
}
