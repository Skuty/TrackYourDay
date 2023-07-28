using MediatR;
using TrackYourDay.Tests.Activities;

namespace TrackYourDay.Tests.ActivityTracking
{
    public class ActivityTracker
    {
        private readonly IPublisher publisher;
        private readonly IStartedActivityRecognizingStrategy startedActivityRecognizingStrategy;
        private readonly IInstantActivityRecognizingStrategy instantActivityRecognizingStrategy;
        private StartedActivity currentStartedActivity;
        private InstantActivity lastInstantActivity;
        private readonly List<EndedActivity> endedActivities;
        private readonly List<InstantActivity> instantActivities;

        public ActivityTracker(
            IPublisher publisher, 
            IStartedActivityRecognizingStrategy activityRecognizingStrategy,
            IInstantActivityRecognizingStrategy instantActivityRecognizingStrategy)
        {
            this.publisher = publisher;
            this.startedActivityRecognizingStrategy = activityRecognizingStrategy;
            this.instantActivityRecognizingStrategy = instantActivityRecognizingStrategy;
            this.endedActivities = new List<EndedActivity>();
            this.instantActivities = new List<InstantActivity>();
        }

        internal void RecognizeEvents()
        {
            throw new NotImplementedException();
        }

        internal StartedActivity? GetCurrentActivity()
        {
            throw new NotImplementedException();
        }

        internal List<EndedActivity> GetEndedActivities()
        {
            throw new NotImplementedException();
        }
    }
}