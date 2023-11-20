using TrackYourDay.Core.Activities.SystemStates;

namespace TrackYourDay.Core.Activities.ActivityRecognizing
{
    public interface IStartedActivityRecognizingStrategy
    {
        public SystemState RecognizeActivity();
    }
}