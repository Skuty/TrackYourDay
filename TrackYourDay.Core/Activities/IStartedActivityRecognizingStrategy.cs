namespace TrackYourDay.Core.Activities
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