namespace TrackYourDay.Tests.Workday
{
    public class BreaksLeftTests
    {
        [Fact]
        public void GivenThereWasNoBreaks_WhenBreaksAreBeingCalculated_ThenBreaksLeftAreEqualTo50Minutes()
        {
        }

        [Fact]
        public void GivenThereWas15MinutesOfBreaks_WhenBreaksAreBeingCalculated_ThenBreaksLeftAreEqualTo35Minutes()
        {
        }

        [Fact]
        public void GivenThereWas50MinutesOfBreaks_WhenBreaksAreBeingCalculated_ThenBreaksLeftAreEqualTo0Minutes()
        {
        }

        // TODO: split in summary activities and breaks to avoid problems with properly calculating break time
        // due to lack of possibility to fully autorecognize breaks - temporarily
    }
}
