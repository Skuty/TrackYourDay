namespace TrackYourDay.Core.Activities
{
    public interface IInstantActivityRecognizingStrategy
    {
        public ActivityType RecognizeActivity();
    }
}