using FluentAssertions;
using Moq;
using TrackYourDay.Core.Persistence;
using TrackYourDay.Infrastructure.Persistence;

namespace TrackYourDay.Tests.Infrastructure.Persistence;

public class SqlCipherConnectionFactoryTests
{
    [Fact]
    public void GivenValidDatabasePath_WhenCreateConnectionString_ThenReturnsConnectionStringWithPassword()
    {
        // Given
        var mockKeyProvider = new Mock<IDatabaseKeyProvider>();
        mockKeyProvider.Setup(x => x.GetDatabaseKey()).Returns("test-key-123");
        var factory = new SqlCipherConnectionFactory(mockKeyProvider.Object);
        var databasePath = "test.db";

        // When
        var connectionString = factory.CreateConnectionString(databasePath);

        // Then
        connectionString.Should().Contain("Data Source=test.db");
        connectionString.Should().Contain("Password=test-key-123");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GivenInvalidDatabasePath_WhenCreateConnectionString_ThenThrowsArgumentException(string? databasePath)
    {
        // Given
        var mockKeyProvider = new Mock<IDatabaseKeyProvider>();
        var factory = new SqlCipherConnectionFactory(mockKeyProvider.Object);

        // When
        var action = () => factory.CreateConnectionString(databasePath!);

        // Then
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GivenNullKeyProvider_WhenCreatingSqlCipherConnectionFactory_ThenThrowsArgumentNullException()
    {
        // Given & When
        var action = () => new SqlCipherConnectionFactory(null!);

        // Then
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WhenCreateConnectionString_ThenCallsGetDatabaseKey()
    {
        // Given
        var mockKeyProvider = new Mock<IDatabaseKeyProvider>();
        mockKeyProvider.Setup(x => x.GetDatabaseKey()).Returns("key");
        var factory = new SqlCipherConnectionFactory(mockKeyProvider.Object);

        // When
        factory.CreateConnectionString("test.db");

        // Then
        mockKeyProvider.Verify(x => x.GetDatabaseKey(), Times.Once);
    }
}
