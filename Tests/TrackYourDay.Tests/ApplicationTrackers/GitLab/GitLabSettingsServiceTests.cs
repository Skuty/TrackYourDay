using FluentAssertions;
using Moq;
using TrackYourDay.Core.ApplicationTrackers.GitLab;
using TrackYourDay.Core.Settings;

namespace TrackYourDay.Tests.ApplicationTrackers.GitLab;

public sealed class GitLabSettingsServiceTests
{
    private readonly Mock<IGenericSettingsService> _settingsServiceMock;
    private readonly GitLabSettingsService _sut;

    public GitLabSettingsServiceTests()
    {
        _settingsServiceMock = new Mock<IGenericSettingsService>();
        _sut = new GitLabSettingsService(_settingsServiceMock.Object);
    }

    [Fact]
    public void GivenNoSettings_WhenGetSettings_ThenReturnsDefaults()
    {
        // Given
        _settingsServiceMock.Setup(x => x.GetEncryptedSetting("GitLab.ApiUrl", string.Empty)).Returns(string.Empty);
        _settingsServiceMock.Setup(x => x.GetEncryptedSetting("GitLab.ApiKey", string.Empty)).Returns(string.Empty);
        _settingsServiceMock.Setup(x => x.GetSetting("GitLab.Enabled", false)).Returns(false);
        _settingsServiceMock.Setup(x => x.GetSetting("GitLab.FetchIntervalMinutes", 15)).Returns(15);
        _settingsServiceMock.Setup(x => x.GetSetting("GitLab.CircuitBreakerThreshold", 5)).Returns(5);
        _settingsServiceMock.Setup(x => x.GetSetting("GitLab.CircuitBreakerDurationMinutes", 5)).Returns(5);
        _settingsServiceMock.Setup(x => x.GetSetting("GitLab.LastSyncTimestamp", string.Empty)).Returns(string.Empty);

        // When
        var result = _sut.GetSettings();

        // Then
        result.ApiUrl.Should().BeEmpty();
        result.ApiKey.Should().BeEmpty();
        result.Enabled.Should().BeFalse();
        result.FetchIntervalMinutes.Should().Be(15);
        result.CircuitBreakerThreshold.Should().Be(5);
        result.CircuitBreakerDurationMinutes.Should().Be(5);
        result.LastSyncTimestamp.Should().BeNull();
    }

    [Fact]
    public void GivenValidLastSyncTimestamp_WhenGetSettings_ThenReturnsTimestamp()
    {
        // Given
        var expectedTimestamp = new DateTime(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        _settingsServiceMock.Setup(x => x.GetEncryptedSetting(It.IsAny<string>(), It.IsAny<string>())).Returns(string.Empty);
        _settingsServiceMock.Setup(x => x.GetSetting(It.IsAny<string>(), It.IsAny<int>())).Returns(15);
        _settingsServiceMock.Setup(x => x.GetSetting(It.IsAny<string>(), It.IsAny<bool>())).Returns(false);
        _settingsServiceMock.Setup(x => x.GetSetting("GitLab.LastSyncTimestamp", string.Empty))
            .Returns(expectedTimestamp.ToString("O"));

        // When
        var result = _sut.GetSettings();

        // Then
        result.LastSyncTimestamp.Should().BeCloseTo(expectedTimestamp, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void GivenInvalidLastSyncTimestamp_WhenGetSettings_ThenReturnsNull()
    {
        // Given
        _settingsServiceMock.Setup(x => x.GetEncryptedSetting(It.IsAny<string>(), It.IsAny<string>())).Returns(string.Empty);
        _settingsServiceMock.Setup(x => x.GetSetting(It.IsAny<string>(), It.IsAny<int>())).Returns(15);
        _settingsServiceMock.Setup(x => x.GetSetting(It.IsAny<string>(), It.IsAny<bool>())).Returns(false);
        _settingsServiceMock.Setup(x => x.GetSetting("GitLab.LastSyncTimestamp", string.Empty))
            .Returns("invalid-date");

        // When
        var result = _sut.GetSettings();

        // Then
        result.LastSyncTimestamp.Should().BeNull();
    }

    [Fact]
    public void GivenApiCredentials_WhenUpdateSettings_ThenPersistsEncryptedValues()
    {
        // Given
        const string apiUrl = "https://gitlab.example.com";
        const string apiKey = "test-api-key";

        // When
        _sut.UpdateSettings(apiUrl, apiKey);

        // Then
        _settingsServiceMock.Verify(x => x.SetEncryptedSetting("GitLab.ApiUrl", apiUrl), Times.Once);
        _settingsServiceMock.Verify(x => x.SetEncryptedSetting("GitLab.ApiKey", apiKey), Times.Once);
    }

    [Fact]
    public void GivenNullApiCredentials_WhenUpdateSettings_ThenPersistsEmptyStrings()
    {
        // Given
        string? apiUrl = null;
        string? apiKey = null;

        // When
        _sut.UpdateSettings(apiUrl!, apiKey!);

        // Then
        _settingsServiceMock.Verify(x => x.SetEncryptedSetting("GitLab.ApiUrl", string.Empty), Times.Once);
        _settingsServiceMock.Verify(x => x.SetEncryptedSetting("GitLab.ApiKey", string.Empty), Times.Once);
    }

    [Fact]
    public void GivenTimestamp_WhenUpdateLastSyncTimestamp_ThenPersistsIso8601Format()
    {
        // Given
        var timestamp = new DateTime(2026, 1, 16, 12, 0, 0, DateTimeKind.Utc);

        // When
        _sut.UpdateLastSyncTimestamp(timestamp);

        // Then
        _settingsServiceMock.Verify(x => x.SetSetting("GitLab.LastSyncTimestamp", timestamp.ToString("O")), Times.Once);
    }

    [Fact]
    public void WhenPersistSettings_ThenDelegatesToGenericSettingsService()
    {
        // When
        _sut.PersistSettings();

        // Then
        _settingsServiceMock.Verify(x => x.PersistSettings(), Times.Once);
    }

    [Fact]
    public void GivenFullConfiguration_WhenUpdateSettingsWithAllParameters_ThenPersistsAllValues()
    {
        // Given
        const string apiUrl = "https://gitlab.example.com";
        const string apiKey = "test-key";
        const bool enabled = true;
        const int fetchInterval = 30;
        const int cbThreshold = 10;
        const int cbDuration = 15;

        // When
        _sut.UpdateSettings(apiUrl, apiKey, enabled, fetchInterval, cbThreshold, cbDuration);

        // Then
        _settingsServiceMock.Verify(x => x.SetEncryptedSetting("GitLab.ApiUrl", apiUrl), Times.Once);
        _settingsServiceMock.Verify(x => x.SetEncryptedSetting("GitLab.ApiKey", apiKey), Times.Once);
        _settingsServiceMock.Verify(x => x.SetSetting("GitLab.Enabled", enabled), Times.Once);
        _settingsServiceMock.Verify(x => x.SetSetting("GitLab.FetchIntervalMinutes", fetchInterval), Times.Once);
        _settingsServiceMock.Verify(x => x.SetSetting("GitLab.CircuitBreakerThreshold", cbThreshold), Times.Once);
        _settingsServiceMock.Verify(x => x.SetSetting("GitLab.CircuitBreakerDurationMinutes", cbDuration), Times.Once);
    }
}
