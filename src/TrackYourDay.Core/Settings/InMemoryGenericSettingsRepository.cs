using System.Collections.Concurrent;

namespace TrackYourDay.Core.Settings
{
    public class InMemoryGenericSettingsRepository : IGenericSettingsRepository
    {
        private readonly ConcurrentDictionary<string, string> settings = new();

        public string? GetSetting(string key)
        {
            return settings.TryGetValue(key, out var value) ? value : null;
        }

        public void SetSetting(string key, string value)
        {
            settings[key] = value;
        }

        public bool HasSetting(string key)
        {
            return settings.ContainsKey(key);
        }

        public void RemoveSetting(string key)
        {
            settings.TryRemove(key, out _);
        }

        public IEnumerable<string> GetAllKeys()
        {
            return settings.Keys.ToList();
        }

        public void Save()
        {
            // In-memory implementation doesn't need to persist
        }

        public void Load()
        {
            // In-memory implementation doesn't need to load
        }

        public void Clear()
        {
            settings.Clear();
        }
    }
}
