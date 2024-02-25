using TrackYourDay.Tests.Old.Old.Activities;

namespace TrackYourDay.Tests.Old.Activities
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
            var distinctActivities = activityEvents.Select(x => x.Activity).Distinct();
            return distinctActivities;
        }

        public TimeSpan GetTimeOfAllActivities()
        {
            var firstActivity = activityEvents.FirstOrDefault();
            var lastActivity = activityEvents.LastOrDefault();

            if (firstActivity == null || lastActivity == null)
            {
                return TimeSpan.Zero;
            };

            return lastActivity.EventDate - firstActivity.EventDate;
        }

        public TimeSpan GetTimeOfSpecificActivity<T>() where T : Activity
        {
            TimeSpan timeOfActivity = TimeSpan.Zero;

            for (int i = 0; i < activityEvents.Count(); i++)
            {
                var currentActivity = activityEvents.ElementAt(i);
                var nextActivity = activityEvents.ElementAtOrDefault(i + 1);

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