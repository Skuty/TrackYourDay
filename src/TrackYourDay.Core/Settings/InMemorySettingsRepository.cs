namespace TrackYourDay.Core.Settings
{
    [Obsolete("Used only for testing purposes on WEB project")]
    public class InMemorySettingsRepository : ISettingsRepository
    {
        private ISettingsSet settingsSet;

        public InMemorySettingsRepository()
        {
            this.settingsSet = new DefaultSettingsSet();
        }

        public void Reset()
        {
            this.settingsSet = new DefaultSettingsSet();
        }

        public ISettingsSet Get()
        {
            return this.settingsSet;
        }

        public void Save(ISettingsSet settings)
        {
            this.settingsSet = settings;
        }
    }
}
