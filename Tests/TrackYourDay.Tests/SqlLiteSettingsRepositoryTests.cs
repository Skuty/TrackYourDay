using FluentAssertions;
using TrackYourDay.Core.ApplicationTrackers.Breaks;
using TrackYourDay.Core.ApplicationTrackers.GitLab;
using TrackYourDay.Core.ApplicationTrackers.Jira;
using TrackYourDay.Core.Insights.Workdays;
using TrackYourDay.Core.Settings;
using TrackYourDay.Core.SystemTrackers;

namespace TrackYourDay.Tests
{
    public class SqlLiteSettingsRepositoryTests 
    {
        SqlLiteSettingsRepository sqlLiteSettingsRepository;
        
        public SqlLiteSettingsRepositoryTests()
        {
            File.Delete("TrackYourDay.db");
            this.sqlLiteSettingsRepository = new SqlLiteSettingsRepository();
        }

        [Fact]
        public void WhenPersistingSettingsAndRetreiving_ThenSettingsHaveSameValues()
        {
            // Arrange
            var settingsToSave = new UserSettingsSet(
                new ActivitiesSettings(TimeSpan.FromSeconds(1)),
                new BreaksSettings(TimeSpan.FromSeconds(2)),
                WorkdayDefinition.CreateDefaultDefinition(),
                GitLabSettings.CreateDefaultSettings(),
                JiraSettings.CreateDefaultSettings());

            // Act
            this.sqlLiteSettingsRepository.Save(settingsToSave);
            var savedSettings = this.sqlLiteSettingsRepository.Get();

            // Assert
            // TODO Change it to equal or record to avoid val by val comparing
            savedSettings.ActivitiesSettings.FrequencyOfActivityDiscovering
                .Should().Be(settingsToSave.ActivitiesSettings.FrequencyOfActivityDiscovering);
            // TODO: Add missing comparasions
        }
    }
}
