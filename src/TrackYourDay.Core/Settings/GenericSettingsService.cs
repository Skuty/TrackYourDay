using System.Text.Json;

namespace TrackYourDay.Core.Settings
{
    public class GenericSettingsService : IGenericSettingsService
    {
        private readonly IGenericSettingsRepository repository;
        private readonly IEncryptionService encryptionService;

        public GenericSettingsService(IGenericSettingsRepository repository, IEncryptionService encryptionService)
        {
            this.repository = repository;
            this.encryptionService = encryptionService;
        }

        public T GetSetting<T>(string key, T defaultValue = default)
        {
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
                    return JsonSerializer.Deserialize<T>(storedValue);
                }

                // Handle simple types
                return (T)Convert.ChangeType(storedValue, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }

        public void SetSetting<T>(string key, T value)
        {
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

            repository.SetSetting(key, serializedValue);
        }

        public string GetEncryptedSetting(string key, string defaultValue = "")
        {
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
            return repository.HasSetting(key);
        }

        public void RemoveSetting(string key)
        {
            repository.RemoveSetting(key);
        }

        public void PersistSettings()
        {
            repository.Save();
        }

        public void LoadSettings()
        {
            repository.Load();
        }

        public void ClearAllSettings()
        {
            repository.Clear();
        }
    }
}
