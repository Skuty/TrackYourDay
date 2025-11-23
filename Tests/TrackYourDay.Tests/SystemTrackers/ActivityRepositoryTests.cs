using TrackYourDay.Core.SystemTrackers;
using TrackYourDay.Core.SystemTrackers.SystemStates;
using Xunit;

namespace TrackYourDay.Tests.SystemTrackers
{
    public class ActivityRepositoryTests
    {
        private string GetTempDatabasePath() => Path.Combine(Path.GetTempPath(), $"test_activities_{Guid.NewGuid()}.db");

        [Fact]
        public void GivenActivityRepository_WhenSavingAndRetrievingActivities_ThenActivitiesAreReturnedCorrectly()
        {
            // Arrange
            var dbPath = GetTempDatabasePath();
            var repository = new SqliteActivityRepository(dbPath);
            
            var date = DateOnly.FromDateTime(DateTime.Now.AddDays(-1));
            var startDate = date.ToDateTime(new TimeOnly(10, 0));
            var endDate = date.ToDateTime(new TimeOnly(10, 30));
            var systemState = SystemStateFactory.FocusOnApplicationState("Test Application");
            var activity = new EndedActivity(startDate, endDate, systemState);

            // Act
            repository.Save(activity);
            var activities = repository.GetActivitiesForDate(date);

            // Assert
            Assert.Single(activities);
            Assert.Equal(startDate, activities.First().StartDate);
            Assert.Equal(endDate, activities.First().EndDate);
            Assert.Contains("Test Application", activities.First().ActivityType.ActivityDescription);
            
            // Cleanup
            repository.Clear();
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }

        [Fact]
        public void GivenActivityRepository_WhenGettingActivitiesBetweenDates_ThenCorrectActivitiesAreReturned()
        {
            // Arrange
            var dbPath = GetTempDatabasePath();
            var repository = new SqliteActivityRepository(dbPath);
            
            var date1 = DateOnly.FromDateTime(DateTime.Now.AddDays(-3));
            var date2 = DateOnly.FromDateTime(DateTime.Now.AddDays(-2));
            var date3 = DateOnly.FromDateTime(DateTime.Now.AddDays(-1));
            
            var activity1 = new EndedActivity(
                date1.ToDateTime(new TimeOnly(10, 0)),
                date1.ToDateTime(new TimeOnly(10, 30)),
                SystemStateFactory.FocusOnApplicationState("App 1"));
            
            var activity2 = new EndedActivity(
                date2.ToDateTime(new TimeOnly(11, 0)),
                date2.ToDateTime(new TimeOnly(11, 30)),
                SystemStateFactory.FocusOnApplicationState("App 2"));
            
            var activity3 = new EndedActivity(
                date3.ToDateTime(new TimeOnly(12, 0)),
                date3.ToDateTime(new TimeOnly(12, 30)),
                SystemStateFactory.FocusOnApplicationState("App 3"));

            // Act
            repository.Save(activity1);
            repository.Save(activity2);
            repository.Save(activity3);
            
            var activities = repository.GetActivitiesBetweenDates(date1, date2);

            // Assert
            Assert.Equal(2, activities.Count);
            Assert.Contains(activities, a => a.ActivityType.ActivityDescription.Contains("App 1"));
            Assert.Contains(activities, a => a.ActivityType.ActivityDescription.Contains("App 2"));
            Assert.DoesNotContain(activities, a => a.ActivityType.ActivityDescription.Contains("App 3"));
            
            // Cleanup
            repository.Clear();
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }

        [Fact]
        public void GivenActivityRepository_WhenGettingDatabaseStats_ThenStatsAreReturned()
        {
            // Arrange
            var dbPath = GetTempDatabasePath();
            var repository = new SqliteActivityRepository(dbPath);
            
            var date = DateOnly.FromDateTime(DateTime.Now.AddDays(-1));
            var activity = new EndedActivity(
                date.ToDateTime(new TimeOnly(10, 0)),
                date.ToDateTime(new TimeOnly(10, 30)),
                SystemStateFactory.FocusOnApplicationState("Test App"));

            // Act
            repository.Save(activity);
            var count = repository.GetTotalRecordCount();
            var size = repository.GetDatabaseSizeInBytes();

            // Assert
            Assert.Equal(1, count);
            Assert.True(size > 0);
            
            // Cleanup
            repository.Clear();
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }
    }
}
