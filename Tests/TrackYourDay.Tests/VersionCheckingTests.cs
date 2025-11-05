using FluentAssertions;
using TrackYourDay.Core.Versioning;

namespace TrackYourDay.Tests
{
    public class VersionCheckingTests
    {
        [Theory]
        [InlineData("v0.0.1", "v0.0.2")]
        [InlineData("v0.0.1", "v1.0.0")]
        [InlineData("v0.0.1", "v1.0.5")]
        [InlineData("v0.0.9", "v1.0.0")]
        [InlineData("v0.0.9", "v1.0.5")]
        [InlineData("v0.0.11", "v0.1.0")]
        [InlineData("v0.1.0", "v1.0.0")]
        [InlineData("v0.1.0", "v0.2.0")]
        [InlineData("v0.1.0", "v0.2.1")]
        [InlineData("0.1.9", "0.2.0")]
        [InlineData("1.0.0", "2.0.0")]
        [InlineData("1.9.9", "2.0.0")]
        public void WhenVersionIsNewer_ThenTrueIsReturned(string olderVersion, string newerVersion)
        {
            // Arrange
            var older = new ApplicationVersion(olderVersion, false);
            var newer = new ApplicationVersion(newerVersion, false);

            // Act
            var isNewerVersion = newer.IsNewerThan(older);

            // Assert
            isNewerVersion.Should().BeTrue();
        }

        [Fact(Skip = "To be implemented in future")]
        public void ReturnsVersionOfCurrentApplication()
        {
            Assert.Fail("Not implemented");
        }

        [Fact]
        public void ReturnsVersionOfNewestAvailableApplication()
        {
            // Arrange
            var versioningSystemFacade = new VersioningSystemFacade(new Version(1, 0));

            // Act
            var result = versioningSystemFacade.GetNewestAvailableApplicationVersion();

            // Assert
            result.Should().NotBeNull();
        }

        [Fact(Skip = "To be implemented in future")]
        public void ReturnsTrueIfNewerVersionIsAvailable()
        {
            Assert.Fail("Not implemented");
        }
    }
}
