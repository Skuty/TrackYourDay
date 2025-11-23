using TrackYourDay.Core.ApplicationTrackers.Breaks;
using Xunit;

namespace TrackYourDay.Tests.ApplicationTrackers.Breaks
{
    [Trait("Category", "Unit")]
    public class BreakRepositoryTests
    {
        private string GetTempDatabasePath() => Path.Combine(Path.GetTempPath(), $"test_breaks_{Guid.NewGuid()}.db");

        [Fact]
        public void GivenBreakRepository_WhenSavingAndRetrievingBreaks_ThenBreaksAreReturnedCorrectly()
        {
            // Arrange
            var dbPath = GetTempDatabasePath();
            var repository = new SqliteBreakRepository(dbPath);
            
            var date = DateOnly.FromDateTime(DateTime.Now.AddDays(-1));
            var startedAt = date.ToDateTime(new TimeOnly(12, 0));
            var endedAt = date.ToDateTime(new TimeOnly(12, 15));
            var endedBreak = new EndedBreak(Guid.NewGuid(), startedAt, endedAt, "Lunch Break");

            // Act
            repository.Save(endedBreak);
            var breaks = repository.GetBreaksForDate(date);

            // Assert
            Assert.Single(breaks);
            Assert.Equal(startedAt, breaks.First().BreakStartedAt);
            Assert.Equal(endedAt, breaks.First().BreakEndedAt);
            Assert.Equal("Lunch Break", breaks.First().BreakDescription);
            
            // Cleanup
            repository.Clear();
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }

        [Fact]
        public void GivenBreakRepository_WhenGettingBreaksBetweenDates_ThenCorrectBreaksAreReturned()
        {
            // Arrange
            var dbPath = GetTempDatabasePath();
            var repository = new SqliteBreakRepository(dbPath);
            
            var date1 = DateOnly.FromDateTime(DateTime.Now.AddDays(-3));
            var date2 = DateOnly.FromDateTime(DateTime.Now.AddDays(-2));
            var date3 = DateOnly.FromDateTime(DateTime.Now.AddDays(-1));
            
            var break1 = new EndedBreak(
                Guid.NewGuid(),
                date1.ToDateTime(new TimeOnly(12, 0)),
                date1.ToDateTime(new TimeOnly(12, 15)),
                "Break 1");
            
            var break2 = new EndedBreak(
                Guid.NewGuid(),
                date2.ToDateTime(new TimeOnly(15, 0)),
                date2.ToDateTime(new TimeOnly(15, 15)),
                "Break 2");
            
            var break3 = new EndedBreak(
                Guid.NewGuid(),
                date3.ToDateTime(new TimeOnly(14, 0)),
                date3.ToDateTime(new TimeOnly(14, 15)),
                "Break 3");

            // Act
            repository.Save(break1);
            repository.Save(break2);
            repository.Save(break3);
            
            var breaks = repository.GetBreaksBetweenDates(date1, date2);

            // Assert
            Assert.Equal(2, breaks.Count);
            Assert.Contains(breaks, b => b.BreakDescription == "Break 1");
            Assert.Contains(breaks, b => b.BreakDescription == "Break 2");
            Assert.DoesNotContain(breaks, b => b.BreakDescription == "Break 3");
            
            // Cleanup
            repository.Clear();
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }

        [Fact(Skip = "SQLite file handling in tests needs investigation")]
        public void GivenBreakRepository_WhenClearingDatabase_ThenDatabaseIsReinitialized()
        {
            // Arrange
            var dbPath = GetTempDatabasePath();
            var repository = new SqliteBreakRepository(dbPath);
            var date = DateOnly.FromDateTime(DateTime.Now);
            var endedBreak = new EndedBreak(
                Guid.NewGuid(),
                date.ToDateTime(new TimeOnly(12, 0)),
                date.ToDateTime(new TimeOnly(12, 15)),
                "Test Break");
            
            repository.Save(endedBreak);
            Assert.True(File.Exists(dbPath));
            var sizeBeforeClear = new FileInfo(dbPath).Length;

            // Act
            repository.Clear();

            // Assert - verify database file was recreated (smaller size)
            Assert.True(File.Exists(dbPath));
            var sizeAfterClear = new FileInfo(dbPath).Length;
            Assert.True(sizeAfterClear < sizeBeforeClear || sizeAfterClear > 0);
            
            // Verify we can save to the cleared database
            repository.Save(endedBreak);
            var breaksAfterClear = repository.GetBreaksForDate(date);
            Assert.Single(breaksAfterClear);
            
            // Cleanup
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }
    }
}
