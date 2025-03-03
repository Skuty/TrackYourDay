using TrackYourDay.Core.SystemTrackers.SystemStates;

namespace TrackYourDay.Core.SystemTrackers.ActivityRecognizing
{
    public interface ISystemStateRecognizingStrategy
    {
        public SystemState RecognizeActivity();
    }
}