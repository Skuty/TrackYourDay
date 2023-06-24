namespace TrackYourDay.Core
{
    public class Features
    {
        public Features(bool isBreakRecordingEnabled)
        {
            this.IsBreakRecordingEnabled = isBreakRecordingEnabled;
        }

        public bool IsBreakRecordingEnabled { get; }
    }
}
