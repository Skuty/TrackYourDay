namespace TrackYourDay.Core.Activities
{
    public abstract record class Activity(string Name);

    public record class SystemLocked() : Activity("System Locked");

    public record class FocusOnApplication() : Activity("Focus on Application");
}
