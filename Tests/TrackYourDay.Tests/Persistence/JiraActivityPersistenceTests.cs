using FluentAssertions;
using TrackYourDay.Core;
using TrackYourDay.Core.ApplicationTrackers.Jira;
using TrackYourDay.Core.Persistence;
using TrackYourDay.Core.Persistence.Specifications;

namespace TrackYourDay.Tests.Persistence
{
    [Trait("Category", "Unit")]
    public class JiraActivityPersistenceTests
    {
        [Fact]
        public void GivenJiraActivityWithGuid_WhenSavedToRepository_ThenCanBeRetrievedByDate()
        {
            // Given
            var clock = new Clock();
            var repository = new GenericDataRepository<JiraActivity>(clock);
            var activity = new JiraActivity(Guid.NewGuid(), DateTime.Today.AddHours(10), "Created Task PROJ-123");
            
            try
            {
                // When
                repository.Save(activity);
                var specification = new JiraActivityByDateSpecification(DateOnly.FromDateTime(DateTime.Today));
                var retrievedActivities = repository.Find(specification);

                // Then
                retrievedActivities.Should().HaveCount(1);
                retrievedActivities.First().Description.Should().Be("Created Task PROJ-123");
                retrievedActivities.First().OccurrenceDate.Should().BeCloseTo(activity.OccurrenceDate, TimeSpan.FromSeconds(1));
            }
            finally
            {
                // Cleanup
                repository.Clear();
            }
        }

        [Fact]
        public void GivenMultipleJiraActivities_WhenSavedToRepository_ThenCanBeRetrievedByDateRange()
        {
            // Given
            var clock = new Clock();
            var repository = new GenericDataRepository<JiraActivity>(clock);
            var activity1 = new JiraActivity(Guid.NewGuid(), DateTime.Today.AddDays(-2).AddHours(10), "Old activity");
            var activity2 = new JiraActivity(Guid.NewGuid(), DateTime.Today.AddHours(10), "Today activity");
            var activity3 = new JiraActivity(Guid.NewGuid(), DateTime.Today.AddDays(1).AddHours(10), "Future activity");
            
            try
            {
                // When
                repository.Save(activity1);
                repository.Save(activity2);
                repository.Save(activity3);
                
                var specification = new JiraActivityByDateRangeSpecification(
                    DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
                    DateOnly.FromDateTime(DateTime.Today));
                var retrievedActivities = repository.Find(specification);

                // Then
                retrievedActivities.Should().HaveCount(1);
                retrievedActivities.First().Description.Should().Be("Today activity");
            }
            finally
            {
                // Cleanup
                repository.Clear();
            }
        }
    }
}
