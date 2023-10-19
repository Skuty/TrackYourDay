namespace TrackYourDay.Tests.Workday
{
    public class WorktimeLeftTests
    {
        [Fact]
        public void GivenThereWasNoActivitiesOrBreaks_WhenWorktimeLeftIsBeingCalculated_ThenWorkTimeLeftIsEqualTo8Hours()
        {
        }

        [Fact]
        public void GivenThereWasNoActivitiesAndThereWas50MinutesOfBreaks_WhenWorktimeLeftIsBeingCalculated_ThenWorkTimeLeftIsEqualTo7HoursAnd10Minutes()
        {
        }

        [Fact]
        public void GivenThereWas7HoursAnd10MinutesMinutesOfActivitiesAndThereWas50MinutesOfBreaks_WhenWorktimeLeftIsBeingCalculated_ThenWorkTimeLeftIsEqualTo0Minutes()
        {
        }

        [Fact]
        public void GivenThereWas7HoursAnd10MinutesMinutesOfActivitiesAndNoBreaks_WhenWorktimeLeftIsBeingCalculated_ThenWorkTimeLeftIsEqualTo0Minutes()
        {
        }

        [Fact]
        public void GivenThereWas3HoursAnd30MinutesMinutesOfActivitiesAndNoBreaks_WhenWorktimeLeftIsBeingCalculated_ThenWorkTimeLeftIsEqualTo3HoursAnd40Minutes()
        {
        }
    }
}
