using TrackYourDay.Core.Settings;
using Moq;

namespace TrackYourDay.Tests.Settings
{
    public class LoggingSettingsServiceTests
    {
        [Fact]
        public void GivenDefaultSettings_WhenGetLoggingSettings_ThenReturnsDefaultValues()
        {
            // Arrange
            var mockGenericSettingsService = new Mock<IGenericSettingsService>();
            mockGenericSettingsService
                .Setup(x => x.GetSetting<LoggingSettings>(It.IsAny<string>(), It.IsAny<LoggingSettings>()))
                .Returns(new LoggingSettings());
            
            var loggingSettingsService = new LoggingSettingsService(mockGenericSettingsService.Object);

            // Act
            var settings = loggingSettingsService.GetLoggingSettings();

            // Assert
            Assert.NotNull(settings);
            Assert.Equal("Information", settings.MinimumLogLevel);
            Assert.True(settings.EnablePerClassLogging);
            Assert.NotEmpty(settings.LogDirectory); // Verify directory is set (platform-specific)
            Assert.Contains("TrackYourDay", settings.LogDirectory); // Verify it contains app name
        }

        [Fact]
        public void GivenCustomSettings_WhenGetLoggingSettings_ThenReturnsCustomValues()
        {
            // Arrange
            var customSettings = new LoggingSettings
            {
                MinimumLogLevel = "Debug",
                EnablePerClassLogging = false,
                LogDirectory = "D:\\CustomLogs"
            };
            
            var mockGenericSettingsService = new Mock<IGenericSettingsService>();
            mockGenericSettingsService
                .Setup(x => x.GetSetting<LoggingSettings>(It.IsAny<string>(), It.IsAny<LoggingSettings>()))
                .Returns(customSettings);
            
            var loggingSettingsService = new LoggingSettingsService(mockGenericSettingsService.Object);

            // Act
            var settings = loggingSettingsService.GetLoggingSettings();

            // Assert
            Assert.NotNull(settings);
            Assert.Equal("Debug", settings.MinimumLogLevel);
            Assert.False(settings.EnablePerClassLogging);
            Assert.Equal("D:\\CustomLogs", settings.LogDirectory);
        }

        [Fact]
        public void GivenLoggingSettings_WhenSaveLoggingSettings_ThenSettingsAreSaved()
        {
            // Arrange
            var mockGenericSettingsService = new Mock<IGenericSettingsService>();
            var loggingSettingsService = new LoggingSettingsService(mockGenericSettingsService.Object);
            
            var settingsToSave = new LoggingSettings
            {
                MinimumLogLevel = "Warning",
                EnablePerClassLogging = true,
                LogDirectory = "C:\\NewLogs"
            };

            // Act
            loggingSettingsService.SaveLoggingSettings(settingsToSave);

            // Assert
            mockGenericSettingsService.Verify(
                x => x.SetSetting("LoggingSettings", settingsToSave),
                Times.Once);
        }

        [Theory]
        [InlineData("Verbose")]
        [InlineData("Debug")]
        [InlineData("Information")]
        [InlineData("Warning")]
        [InlineData("Error")]
        [InlineData("Fatal")]
        public void GivenDifferentLogLevels_WhenSaveLoggingSettings_ThenCorrectLevelIsSaved(string logLevel)
        {
            // Arrange
            var mockGenericSettingsService = new Mock<IGenericSettingsService>();
            var loggingSettingsService = new LoggingSettingsService(mockGenericSettingsService.Object);
            
            var settings = new LoggingSettings { MinimumLogLevel = logLevel };

            // Act
            loggingSettingsService.SaveLoggingSettings(settings);

            // Assert
            mockGenericSettingsService.Verify(
                x => x.SetSetting("LoggingSettings", It.Is<LoggingSettings>(s => s.MinimumLogLevel == logLevel)),
                Times.Once);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GivenPerClassLoggingToggle_WhenSaveLoggingSettings_ThenCorrectValueIsSaved(bool enablePerClassLogging)
        {
            // Arrange
            var mockGenericSettingsService = new Mock<IGenericSettingsService>();
            var loggingSettingsService = new LoggingSettingsService(mockGenericSettingsService.Object);
            
            var settings = new LoggingSettings { EnablePerClassLogging = enablePerClassLogging };

            // Act
            loggingSettingsService.SaveLoggingSettings(settings);

            // Assert
            mockGenericSettingsService.Verify(
                x => x.SetSetting("LoggingSettings", It.Is<LoggingSettings>(s => s.EnablePerClassLogging == enablePerClassLogging)),
                Times.Once);
        }
    }
}
