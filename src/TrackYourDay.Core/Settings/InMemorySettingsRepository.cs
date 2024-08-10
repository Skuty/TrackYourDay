namespace TrackYourDay.Core.Settings
{
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
