using TrackYourDay.Tests.Activities;

namespace TrackYourDay.Tests.ActivityTracking
{
    public interface IStartedActivityRecognizingStrategy
    {
        public StartedActivity RecognizeActivity();
    }

    public interface IInstantActivityRecognizingStrategy
    {
        public InstantActivity RecognizeActivity();
    }
}