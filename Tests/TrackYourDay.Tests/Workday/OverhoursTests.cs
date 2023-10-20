namespace TrackYourDay.Tests.Workday
{
    public class OverhoursTests
    {
        [Fact]
        public void GivenThereWasNoActivitiesOrBreaks_WhenOverhoursAreBeingCalculated_ThenOverhoursEqualsTo0Hours()
        {
        }

        [Fact]
        public void GivenThereWas7HoursAnd10MinutesOfActivitiesAnd50MinutesOfBreaks_WhenOverhoursAreBeingCalculated_ThenOverhoursEqualsTo0Hours()
        {
        }

        [Fact]
        public void GivenThereWas8HoursOfActivitiesAnd50MinutesOfBreaks_WhenOverhoursAreBeingCalculated_ThenOverhoursEqualsTo50Minutes()
        {
        }

        [Fact]
        public void GivenThereWas9HoursAnd40MinutesOfActivitiesAnd50MinutesOfBreaks_WhenOverhoursAreBeingCalculated_ThenOverhoursEqualsTo2HoursAnd30Minutes()
        {
        }
    }
}
