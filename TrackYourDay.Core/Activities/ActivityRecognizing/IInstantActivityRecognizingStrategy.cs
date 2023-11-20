using TrackYourDay.Core.Activities.SystemStates;

namespace TrackYourDay.Core.Activities.ActivityRecognizing
{
    public interface IInstantActivityRecognizingStrategy
    {
        public SystemState RecognizeActivity();
    }
}