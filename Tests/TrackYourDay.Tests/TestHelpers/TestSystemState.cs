using TrackYourDay.Core.SystemTrackers.SystemStates;

namespace TrackYourDay.Tests.TestHelpers
{
    public sealed record TestSystemState(string Description) : SystemState(Description);
}
