using TrackYourDay.Core.Insights.Analytics;
using TrackYourDay.Core.SystemTrackers.SystemStates;

namespace TrackYourDay.Core.SystemTrackers
{
    /// <summary>
    /// Represents a completed system-level activity (app focus, window changes, etc.).
    /// </summary>
    public sealed class EndedActivity : TrackedActivity
    {
        public SystemState ActivityType { get; init; }
        
        public EndedActivity(DateTime startDate, DateTime endDate, SystemState activityType)
            : base(Guid.NewGuid(), startDate, endDate)
        {
            ActivityType = activityType ?? throw new ArgumentNullException(nameof(activityType));
        }
        
        public override string GetDescription()
        {
            return ActivityType.ActivityDescription;
        }
    }
}