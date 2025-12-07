using FluentAssertions;
using TrackYourDay.Core;
using TrackYourDay.Core.ApplicationTrackers.GitLab;
using TrackYourDay.Core.Persistence;
using TrackYourDay.Core.Persistence.Specifications;

namespace TrackYourDay.Tests.Persistence
{
    [Trait("Category", "Unit")]
    public class GitLabActivityPersistenceTests
    {
        [Fact]
        public void GivenGitLabActivityWithGuid_WhenSavedToRepository_ThenCanBeRetrievedByDate()
        {
            // Given
            var clock = new Clock();
            var repository = new GenericDataRepository<GitLabActivity>(clock);
            var activity = new GitLabActivity(Guid.NewGuid(), DateTime.Today.AddHours(10), "Test commit to master");
            
            try
            {
                // When
                repository.Save(activity);
                var specification = new GitLabActivityByDateSpecification(DateOnly.FromDateTime(DateTime.Today));
                var retrievedActivities = repository.Find(specification);

                // Then
                retrievedActivities.Should().HaveCount(1);
                retrievedActivities.First().Description.Should().Be("Test commit to master");
                retrievedActivities.First().OccuranceDate.Should().BeCloseTo(activity.OccuranceDate, TimeSpan.FromSeconds(1));
            }
            finally
            {
                // Cleanup
                repository.Clear();
            }
        }

        [Fact]
        public void GivenMultipleGitLabActivities_WhenSavedToRepository_ThenCanBeRetrievedByDateRange()
        {
            // Given
            var clock = new Clock();
            var repository = new GenericDataRepository<GitLabActivity>(clock);
            var activity1 = new GitLabActivity(Guid.NewGuid(), DateTime.Today.AddDays(-2).AddHours(10), "Old commit");
            var activity2 = new GitLabActivity(Guid.NewGuid(), DateTime.Today.AddHours(10), "Today commit");
            var activity3 = new GitLabActivity(Guid.NewGuid(), DateTime.Today.AddDays(1).AddHours(10), "Future commit");
            
            try
            {
                // When
                repository.Save(activity1);
                repository.Save(activity2);
                repository.Save(activity3);
                
                var specification = new GitLabActivityByDateRangeSpecification(
                    DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
                    DateOnly.FromDateTime(DateTime.Today));
                var retrievedActivities = repository.Find(specification);

                // Then
                retrievedActivities.Should().HaveCount(1);
                retrievedActivities.First().Description.Should().Be("Today commit");
            }
            finally
            {
                // Cleanup
                repository.Clear();
            }
        }
    }
}
