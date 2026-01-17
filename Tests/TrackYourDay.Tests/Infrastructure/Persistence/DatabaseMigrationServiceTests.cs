using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Moq;
using TrackYourDay.Core.Persistence;
using TrackYourDay.Infrastructure.Persistence;

namespace TrackYourDay.Tests.Infrastructure.Persistence;

public class DatabaseMigrationServiceTests
{
    private readonly Mock<IDatabaseKeyProvider> _mockKeyProvider;
    private readonly Mock<ILogger<DatabaseMigrationService>> _mockLogger;
    private readonly DatabaseMigrationService _sut;
    private readonly string _testDbPath;

    public DatabaseMigrationServiceTests()
    {
        _mockKeyProvider = new Mock<IDatabaseKeyProvider>();
        _mockKeyProvider.Setup(x => x.GetDatabaseKey()).Returns("test-key-base64-encoded==");
        _mockLogger = new Mock<ILogger<DatabaseMigrationService>>();
        _sut = new DatabaseMigrationService(_mockKeyProvider.Object, _mockLogger.Object);
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid()}.db");
    }

    [Fact]
    public async Task GivenNonExistentDatabase_WhenMigrateToEncryptedAsync_ThenLogsAndReturns()
    {
        // Given
        var nonExistentPath = "non-existent.db";

        // When
        await _sut.MigrateToEncryptedAsync(nonExistentPath);

        // Then
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("does not exist")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GivenInvalidDatabasePath_WhenMigrateToEncryptedAsync_ThenThrowsArgumentException(string? databasePath)
    {
        // Given & When
        var action = async () => await _sut.MigrateToEncryptedAsync(databasePath!);

        // Then
        await action.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void GivenNonExistentDatabase_WhenIsEncrypted_ThenReturnsFalse()
    {
        // Given
        var nonExistentPath = "non-existent.db";

        // When
        var result = _sut.IsEncrypted(nonExistentPath);

        // Then
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GivenInvalidDatabasePath_WhenIsEncrypted_ThenThrowsArgumentException(string? databasePath)
    {
        // Given & When
        var action = () => _sut.IsEncrypted(databasePath!);

        // Then
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GivenUnencryptedDatabase_WhenIsEncrypted_ThenReturnsFalse()
    {
        // Given
        CreateUnencryptedTestDatabase();

        try
        {
            // When
            var result = _sut.IsEncrypted(_testDbPath);

            // Then
            result.Should().BeFalse();
        }
        finally
        {
            // Cleanup - ensure connection is closed
            GC.Collect();
            GC.WaitForPendingFinalizers();
            
            if (File.Exists(_testDbPath))
            {
                try
                {
                    File.Delete(_testDbPath);
                }
                catch (IOException)
                {
                    // File may still be locked by SQLite
                }
            }
        }
    }

    private void CreateUnencryptedTestDatabase()
    {
        using var connection = new SqliteConnection($"Data Source={_testDbPath}");
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE test (id INTEGER PRIMARY KEY)";
        command.ExecuteNonQuery();
    }
}
