namespace TrackYourDay.Core.SystemTrackers.SystemStates
{
    /// <summary>
    /// Represents base of System State like FocFocus on Application, Mouse moved, etc.
    /// </summary>
    /// <remarks>
    /// Probably still have to be splitted or refactored as it does not represent ie. GitLab commit
    /// Previous approach was to treat it like Activity but it was also not The Thing
    /// SystemState and SystemEvent may be proper
    /// </remarks>
    public abstract record class SystemState(string ActivityDescription);
}