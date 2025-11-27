using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http.Headers;

namespace TrackYourDay.Core.Versioning
{
    public class VersioningSystemFacade
    {
        private ApplicationVersion newestAvailableApplicationVersion = null!;
        private ApplicationVersion currentApplicationVersion = null!;
        private string newestAvailableVersionTagName = null!;

        public VersioningSystemFacade(Version assemblyVersion)
        {
            this.currentApplicationVersion = new ApplicationVersion(assemblyVersion, false);
        }

        public ApplicationVersion GetCurrentApplicationVersion()
        {
            return this.currentApplicationVersion ?? new ApplicationVersion(0, 0, 0, false);
        }

        public ApplicationVersion GetNewestAvailableApplicationVersion()
        {
            try
            {
                if (this.newestAvailableApplicationVersion == null)
                {
                    var releaseInfo = this.GetNewestReleaseInfoFromGitHubRepositoryUsingRestApi();
                    this.newestAvailableApplicationVersion = new ApplicationVersion(releaseInfo.name, releaseInfo.prerelease);
                    this.newestAvailableVersionTagName = releaseInfo.tag_name;
                }

                return this.newestAvailableApplicationVersion;
            }
            catch
            {
                return this.GetCurrentApplicationVersion();
            }
        }

        public string GetWhatsNewForNewestAvailableApplicationVersion()
        {
            var url = "https://api.github.com/repos/skuty/TrackYourDay/releases";

            using var client = CreateGitHubClient(url);
            HttpResponseMessage response = client.GetAsync(url).Result;

            if (response.IsSuccessStatusCode)
            {
                var json = response.Content.ReadAsStringAsync().Result;
                var result = JsonConvert.DeserializeObject<List<GitHubReleaseResponse>>(json);
                return result.OrderByDescending(v => v.published_at).FirstOrDefault()?.body ?? string.Empty;
            }

            throw new Exception("Cannot get newest release information from GitHub repository.");
        }

        public bool IsNewerVersionAvailable()
        {
            var newestVersion = this.GetNewestAvailableApplicationVersion();
            return newestVersion.IsNewerThan(this.GetCurrentApplicationVersion());
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
                    Arguments = this.newestAvailableVersionTagName ?? string.Empty,
                    UseShellExecute = false,
                    WorkingDirectory = appDirectory
                };

                Process.Start(startInfo);
                Process.GetCurrentProcess().Kill();
            }
            else
            {
                throw new ArgumentNullException("ApplicationUpdater", "Updater Application was not found.");
            }
        }

        private GitHubReleaseResponse GetNewestReleaseInfoFromGitHubRepositoryUsingRestApi()
        {
            var url = "https://api.github.com/repos/skuty/TrackYourDay/releases";

            using var client = CreateGitHubClient(url);
            HttpResponseMessage response = client.GetAsync(url).Result;

            if (response.IsSuccessStatusCode)
            {
                var json = response.Content.ReadAsStringAsync().Result;
                var result = JsonConvert.DeserializeObject<List<GitHubReleaseResponse>>(json);

                return result.OrderByDescending(v => v.published_at).FirstOrDefault() 
                    ?? throw new Exception("No releases found in GitHub repository.");
            }

            throw new Exception("Cannot get newest release information from GitHub repository.");
        }

        private HttpClient CreateGitHubClient(string url)
        {
            var client = new HttpClient
            {
                BaseAddress = new Uri(url)
            };

            var productValue = new ProductInfoHeaderValue("TrackYourDay", this.GetCurrentApplicationVersion().ToString());
            client.DefaultRequestHeaders.UserAgent.Add(productValue);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

            return client;
        }
    }
}
