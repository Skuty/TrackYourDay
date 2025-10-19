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
            var older = new ApplicationVersion(olderVersion);
            var newer = new ApplicationVersion(newerVersion);

            // Act
            var isNewerVersion = newer.IsNewerThan(older);

            // Assert
            isNewerVersion.Should().BeTrue();
        }

        [Theory]
        [InlineData("1.0.0-beta.1", "1.0.0-beta.2")]
        [InlineData("1.0.0-alpha", "1.0.0-beta")]
        [InlineData("1.0.0-alpha.1", "1.0.0-alpha.2")]
        [InlineData("1.0.0-beta.1", "1.0.0")]
        [InlineData("1.0.0-rc.1", "1.0.0")]
        [InlineData("2.0.0-beta.1", "2.0.0-beta.2")]
        public void WhenPrereleaseVersionIsNewer_ThenTrueIsReturned(string olderVersion, string newerVersion)
        {
            // Arrange
            var older = new ApplicationVersion(olderVersion);
            var newer = new ApplicationVersion(newerVersion);

            // Act
            var isNewerVersion = newer.IsNewerThan(older);

            // Assert
            isNewerVersion.Should().BeTrue($"{newerVersion} should be newer than {olderVersion}");
        }

        [Theory]
        [InlineData("1.0.0", "1.0.0-beta.1")]
        [InlineData("1.0.0-beta.2", "1.0.0-beta.1")]
        [InlineData("1.0.0-beta", "1.0.0-alpha")]
        [InlineData("2.0.0", "1.0.0")]
        [InlineData("1.0.0", "1.0.0")]
        public void WhenPrereleaseVersionIsNotNewer_ThenFalseIsReturned(string olderVersion, string newerVersion)
        {
            // Arrange
            var older = new ApplicationVersion(olderVersion);
            var newer = new ApplicationVersion(newerVersion);

            // Act
            var isNewerVersion = newer.IsNewerThan(older);

            // Assert
            isNewerVersion.Should().BeFalse($"{newerVersion} should not be newer than {olderVersion}");
        }

        [Theory]
        [InlineData("1.0.0-beta.1", true)]
        [InlineData("1.0.0-alpha", true)]
        [InlineData("1.0.0-rc.1", true)]
        [InlineData("2.0.0-beta-20231201", true)]
        [InlineData("1.0.0", false)]
        [InlineData("2.5.3", false)]
        public void WhenVersionHasPrereleaseIdentifier_IsPrereleaseShouldReturnCorrectValue(string version, bool expectedIsPrerelease)
        {
            // Arrange
            var appVersion = new ApplicationVersion(version);

            // Act
            var isPrerelease = appVersion.IsPrerelease;

            // Assert
            isPrerelease.Should().Be(expectedIsPrerelease);
        }

        [Theory]
        [InlineData("1.0.0-beta.1", "1.0.0-beta.1")]
        [InlineData("1.0.0-alpha", "1.0.0-alpha")]
        [InlineData("1.0.0", "1.0.0")]
        [InlineData("2.5.3-rc.2", "2.5.3-rc.2")]
        public void WhenVersionIsParsed_ToStringReturnsOriginalFormat(string version, string expected)
        {
            // Arrange
            var appVersion = new ApplicationVersion(version);

            // Act
            var result = appVersion.ToString();

            // Assert
            result.Should().Be(expected);
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
