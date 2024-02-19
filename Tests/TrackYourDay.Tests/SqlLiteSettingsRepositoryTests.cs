using FluentAssertions;
using TrackYourDay.Core;
using TrackYourDay.Core.Activities;
using TrackYourDay.Core.Breaks;
using TrackYourDay.Core.Settings;

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
                WorkdayDefinition.CreateDefaultDefinition());

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
