using FluentAssertions;
using TrackYourDay.Infrastructure.Persistence;

namespace TrackYourDay.Tests.Infrastructure.Persistence;

public class WindowsDatabaseKeyProviderTests
{
    [Fact]
    public void WhenGetDatabaseKey_ThenReturnsDeterministicKey()
    {
        // Given
        var provider = new WindowsDatabaseKeyProvider();

        // When
        var key1 = provider.GetDatabaseKey();
        var key2 = provider.GetDatabaseKey();

        // Then
        key1.Should().NotBeNullOrWhiteSpace();
        key2.Should().Be(key1);
    }

    [Fact]
    public void WhenGetDatabaseKey_ThenReturnsBase64EncodedString()
    {
        // Given
        var provider = new WindowsDatabaseKeyProvider();

        // When
        var key = provider.GetDatabaseKey();

        // Then
        var action = () => Convert.FromBase64String(key);
        action.Should().NotThrow();
    }

    [Fact]
    public void WhenGetDatabaseKey_ThenReturns32ByteKey()
    {
        // Given
        var provider = new WindowsDatabaseKeyProvider();

        // When
        var key = provider.GetDatabaseKey();
        var bytes = Convert.FromBase64String(key);

        // Then
        bytes.Length.Should().Be(32);
    }
}
