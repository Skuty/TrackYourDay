using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http.Headers;
using TrackYourDay.Core.Settings;

namespace TrackYourDay.Core.Versioning
{
    public class VersioningSystemFacade
    {
        private const string UpdateChannelSettingKey = "UpdateChannel";
        private ApplicationVersion newestAvailableApplicationVersion = null!;
        private ApplicationVersion newestAvailablePrereleaseVersion = null!;
        private ApplicationVersion currentApplicationVersion = null!;
        private UpdateChannel currentChannel = UpdateChannel.Stable;
        private readonly IGenericSettingsService? settingsService;

        public VersioningSystemFacade(Version assemblyVersion, IGenericSettingsService settingsService = null)
        {
            this.currentApplicationVersion = new ApplicationVersion(assemblyVersion);
            this.settingsService = settingsService;

            // Load saved channel preference
            if (settingsService != null)
            {
                var savedChannel = settingsService.GetSetting<string>(UpdateChannelSettingKey, UpdateChannel.Stable.ToString());
                if (Enum.TryParse<UpdateChannel>(savedChannel, out var channel))
                {
                    this.currentChannel = channel;
                }
            }
        }

        public UpdateChannel GetCurrentChannel()
        {
            return this.currentChannel;
        }

        public void SetChannel(UpdateChannel channel)
        {
            this.currentChannel = channel;

            // Persist the channel preference
            if (settingsService != null)
            {
                settingsService.SetSetting(UpdateChannelSettingKey, channel.ToString());
                settingsService.PersistSettings();
            }

            // Clear cached version to force refresh on next check
            this.newestAvailableApplicationVersion = null!;
            this.newestAvailablePrereleaseVersion = null!;
        }

        public ApplicationVersion GetCurrentApplicationVersion()
        {
            return this.currentApplicationVersion ?? new ApplicationVersion(0, 0, 0);
        }

        public ApplicationVersion GetNewestAvailableApplicationVersion()
        {
            return GetNewestAvailableApplicationVersion(this.currentChannel);
        }

        public ApplicationVersion GetNewestAvailableApplicationVersion(UpdateChannel channel)
        {
            try
            {
                if (channel == UpdateChannel.Stable)
                {
                    if (this.newestAvailableApplicationVersion == null)
                    {
                        var versionFromGitHub = this.GetNewestReleaseNameFromGitHubRepositoryUsingRestApi(false);
                        this.newestAvailableApplicationVersion = new ApplicationVersion(versionFromGitHub);
                    }
                    return this.newestAvailableApplicationVersion;
                }
                else
                {
                    if (this.newestAvailablePrereleaseVersion == null)
                    {
                        var versionFromGitHub = this.GetNewestReleaseNameFromGitHubRepositoryUsingRestApi(true);
                        this.newestAvailablePrereleaseVersion = new ApplicationVersion(versionFromGitHub);
                    }
                    return this.newestAvailablePrereleaseVersion;
                }
            } catch
            {
                return this.GetCurrentApplicationVersion();
            }
        }

        public void ForceRefreshVersionCheck()
        {
            this.newestAvailableApplicationVersion = null!;
            this.newestAvailablePrereleaseVersion = null!;
        }

        public string GetWhatsNewForNewestAvailableApplicationVersion()
        {
            return GetWhatsNewForNewestAvailableApplicationVersion(this.currentChannel);
        }

        public string GetWhatsNewForNewestAvailableApplicationVersion(UpdateChannel channel)
        {
            var url = "https://api.github.com/repos/skuty/TrackYourDay/releases";

            // TODO: Replace with injected HttpClient from IHttpClientFactory
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(url);
            var productValue = new ProductInfoHeaderValue("TrackYourDay", this.GetCurrentApplicationVersion().ToString());

            client.DefaultRequestHeaders.UserAgent.Add(productValue);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

            HttpResponseMessage response = client.GetAsync(url).Result;


            if (response.IsSuccessStatusCode)
            {
                var json = response.Content.ReadAsStringAsync().Result;
                var result = JsonConvert.DeserializeObject<List<GitHubReleaseResponse>>(json);

                bool includePrereleases = channel == UpdateChannel.Prerelease;
                var filteredReleases = includePrereleases
                    ? result.OrderByDescending(v => v.published_at)
                    : result.Where(v => v.prerelease == false).OrderByDescending(v => v.published_at);

                return filteredReleases.FirstOrDefault()?.body ?? string.Empty;
            }

            throw new Exception("Cannot get newest release name from GitHub repository.");
        }

        public bool IsNewerVersionAvailable()
        {
            return this.GetNewestAvailableApplicationVersion().IsNewerThan(this.GetCurrentApplicationVersion());
        }


        public void UpdateApplication()
        {
            string appDirectory = AppDomain.CurrentDomain.BaseDirectory;

            string batchFilePath = Path.Combine(appDirectory, "UpdateApplication.bat");

            if (File.Exists(batchFilePath))
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = batchFilePath,
                    Arguments = this.currentChannel.ToString(), // Pass the current channel as argument
                    UseShellExecute = false, // Set to true to allow batch script execution
                    WorkingDirectory = appDirectory // Ensure it's executed in the app's directory
                };

                Process.Start(startInfo);

                Process.GetCurrentProcess().Kill();
            }
            else
            {
                throw new ArgumentNullException("ApplicationUpdater", "Updater Application was not found.");
            }
        }

        private string GetNewestReleaseNameFromGitHubRepositoryUsingRestApi(bool includePrereleases)
        {
            var url = "https://api.github.com/repos/skuty/TrackYourDay/releases";

            // TODO: Replace with injected HttpClient from IHttpClientFactory
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(url);
            var productValue = new ProductInfoHeaderValue("TrackYourDay", this.GetCurrentApplicationVersion().ToString());

            client.DefaultRequestHeaders.UserAgent.Add(productValue);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

            HttpResponseMessage response = client.GetAsync(url).Result;


            if (response.IsSuccessStatusCode)
            {
                var json = response.Content.ReadAsStringAsync().Result;
                var result = JsonConvert.DeserializeObject<List<GitHubReleaseResponse>>(json);

                var filteredReleases = includePrereleases
                    ? result.OrderByDescending(v => v.published_at)
                    : result.Where(v => v.prerelease == false).OrderByDescending(v => v.published_at);

                return filteredReleases.FirstOrDefault()?.name ?? "0.0.0";
            }

            throw new Exception("Cannot get newest release name from GitHub repository.");
        }
    }
}
