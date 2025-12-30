using System.Collections.Generic;
using System;
using TrackYourDay.Core.SystemTrackers;

namespace TrackYourDay.Core.Insights.Analytics
{
    public interface ISummaryStrategy : IDisposable
    {
        IReadOnlyCollection<GroupedActivity> Generate(IEnumerable<TrackableItem> items);
        string StrategyName { get; }
    }
}
