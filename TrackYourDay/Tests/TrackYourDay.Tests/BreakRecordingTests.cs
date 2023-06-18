using TrackYourDay.Core;

namespace TrackYourDay.Tests
{
    public class BreakRecordingTests
    {
        private readonly Features features;

        public BreakRecordingTests()
        {
            this.features = new Features(isBreakRecordingEnabled: true);
        }

        [Fact]

        public void GivenBreakRecordingFeatureIsEnabled_WhenThereAreNoEventsInSpecifiedAmountOfTime_ThenBreakIsStarted()
        {
            
        }

        [Fact]
        public void GivenBreakRecordingFeatureIsEnabled_WhenUserSessionInOperatingSystemIsBlocked_ThenBreakIsRecorded()
        {
            Assert.Fail("Feature not implemented");
        }

        [Fact]
        public void GivenBreakRecordingFeatureIsEnabled_WhenBreakRecordingEnds_ThenUserCanChooseToRegisterRecordedBreak()
        {
            Assert.Fail("Feature not implemented");
        }
    }
}