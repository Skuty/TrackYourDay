using TrackYourDay.Tests.Activities;

namespace TrackYourDay.Tests.ActivityTracking
{
    public interface IStartedActivityRecognizingStrategy
    {
        public ActivityType RecognizeActivity();
    }

    public interface IInstantActivityRecognizingStrategy
    {
        public ActivityType RecognizeActivity();
    }
}