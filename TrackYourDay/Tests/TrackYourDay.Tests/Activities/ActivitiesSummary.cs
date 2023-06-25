using TrackYourDay.Core.Activities;

namespace TrackYourDay.Tests.Activities
{
    internal class ActivitiesSummary
    {
        private readonly IEnumerable<ActivityEvent> activityEvents;

        public ActivitiesSummary(IEnumerable<ActivityEvent> activityEvents)
        {
            this.activityEvents = activityEvents;
        }

        public IEnumerable<Activity> GetListOfActivities()
        {
            var distinctActivities = this.activityEvents.Select(x => x.Activity).Distinct();
            return distinctActivities;
        }

        public TimeSpan GetTimeOfAllActivities()
        {
            var firstActivity = this.activityEvents.FirstOrDefault();
            var lastActivity = this.activityEvents.LastOrDefault();

            if (firstActivity == null || lastActivity == null)
            {
                return TimeSpan.Zero;
            };

            return lastActivity.EventDate - firstActivity.EventDate;
        }

        public TimeSpan GetTimeOfSpecificActivity<T>() where T : Activity
        {
            TimeSpan timeOfActivity = TimeSpan.Zero;

            for (int i = 0; i < this.activityEvents.Count(); i++)
            {
                var currentActivity = this.activityEvents.ElementAt(i);
                var nextActivity = this.activityEvents.ElementAtOrDefault(i + 1);
                
                if (currentActivity.Activity.GetType() == typeof(T))
                {
                    if (nextActivity != null && nextActivity.Activity.GetType() != typeof(T))
                    {
                        timeOfActivity += nextActivity.EventDate - currentActivity.EventDate;
                    }
                }
            }

            return timeOfActivity;
        }
    }
}