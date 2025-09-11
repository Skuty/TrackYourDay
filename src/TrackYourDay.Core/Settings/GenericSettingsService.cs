using System.Text.Json;

namespace TrackYourDay.Core.Settings
{
    public class GenericSettingsService : IGenericSettingsService
    {
        private readonly IGenericSettingsRepository repository;
        private readonly IEncryptionService encryptionService;
        private readonly Dictionary<string, object> cache = new();
        private bool isLoaded = false;

        public GenericSettingsService(IGenericSettingsRepository repository, IEncryptionService encryptionService)
        {
            this.repository = repository;
            this.encryptionService = encryptionService;
        }

        public T GetSetting<T>(string key, T defaultValue = default)
        {
            EnsureLoaded();

            if (cache.TryGetValue(key, out var cachedValue))
            {
                if (cachedValue is T typedValue)
                {
                    return typedValue;
                }
                
                // Try to convert if types don't match exactly
                try
                {
                    return (T)Convert.ChangeType(cachedValue, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }

            var storedValue = repository.GetSetting(key);
            if (storedValue == null)
            {
                return defaultValue;
            }

            try
            {
                // Try to deserialize as JSON first (for complex objects)
                if (typeof(T) != typeof(string) && !typeof(T).IsPrimitive && typeof(T) != typeof(DateTime))
                {
                    var deserializedValue = JsonSerializer.Deserialize<T>(storedValue);
                    cache[key] = deserializedValue;
                    return deserializedValue;
                }

                // Handle simple types
                var convertedValue = (T)Convert.ChangeType(storedValue, typeof(T));
                cache[key] = convertedValue;
                return convertedValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        public void SetSetting<T>(string key, T value)
        {
            EnsureLoaded();

            string serializedValue;
            
            if (value == null)
            {
                serializedValue = string.Empty;
            }
            else if (typeof(T) == typeof(string) || typeof(T).IsPrimitive || typeof(T) == typeof(DateTime))
            {
                serializedValue = value.ToString();
            }
            else
            {
                serializedValue = JsonSerializer.Serialize(value);
            }

            cache[key] = value;
            repository.SetSetting(key, serializedValue);
        }

        public string GetEncryptedSetting(string key, string defaultValue = "")
        {
            EnsureLoaded();

            var encryptedValue = repository.GetSetting(key);
            if (string.IsNullOrEmpty(encryptedValue))
            {
                return defaultValue;
            }

            try
            {
                return encryptionService.Decrypt(encryptedValue);
            }
            catch
            {
                return defaultValue;
            }
        }

        public void SetEncryptedSetting(string key, string value)
        {
            EnsureLoaded();

            if (string.IsNullOrEmpty(value))
            {
                repository.SetSetting(key, string.Empty);
            }
            else
            {
                var encryptedValue = encryptionService.Encrypt(value);
                repository.SetSetting(key, encryptedValue);
            }
        }

        public bool HasSetting(string key)
        {
            EnsureLoaded();
            return repository.HasSetting(key);
        }

        public void RemoveSetting(string key)
        {
            EnsureLoaded();
            cache.Remove(key);
            repository.RemoveSetting(key);
        }

        public void PersistSettings()
        {
            repository.Save();
        }

        public void LoadSettings()
        {
            repository.Load();
            cache.Clear();
            isLoaded = true;
        }

        public void ClearAllSettings()
        {
            cache.Clear();
            repository.Clear();
            isLoaded = true;
        }

        private void EnsureLoaded()
        {
            if (!isLoaded)
            {
                LoadSettings();
            }
        }
    }
}
