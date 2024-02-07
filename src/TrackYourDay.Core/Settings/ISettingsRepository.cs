namespace TrackYourDay.Core.Settings
{
    public interface ISettingsRepository
    {
        public ISettingsSet Get();

        public void Save(ISettingsSet settings);
    }
}
