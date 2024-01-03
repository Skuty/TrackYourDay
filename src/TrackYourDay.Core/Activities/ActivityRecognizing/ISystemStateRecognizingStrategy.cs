using TrackYourDay.Core.Activities.SystemStates;

namespace TrackYourDay.Core.Activities.ActivityRecognizing
{
    public interface ISystemStateRecognizingStrategy
    {
        public SystemState RecognizeActivity();
    }
}