namespace TrackYourDay.Tests.Activities
{
    [Trait("Category", "Unit")]
    public class CustomActivityTests
    {
        public void FocusOnApplicationActivityIsStarted_()
        {
            throw new System.NotImplementedException();
        }

        public void WhenInstanctActivityIsCreated_ThenItHaveOccuranceDate()
        {
            // Arrange
            var occuranceDate = DateTime.Parse("2008-01-10");

            // Act
            var activity = Activity.Instant(occuranceDate);

            // Assert
            activity.StartDate.Should().Be(occuranceDate);
            activity.EndDate.Should().Be(occuranceDate);
            activity.Duration.Should().Be(TimeSpan.Zero);
        }
    }
}
