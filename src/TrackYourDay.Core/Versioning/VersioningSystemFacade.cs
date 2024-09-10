using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http.Headers;

namespace TrackYourDay.Core.Versioning
{
    public class VersioningSystemFacade
    {
        private ApplicationVersion newestAvailableApplicationVersion = null!;
        private ApplicationVersion currentApplicationVersion = null!;

        public VersioningSystemFacade(Version assemblyVersion)
        {
            this.currentApplicationVersion = new ApplicationVersion(assemblyVersion);
        }

        public ApplicationVersion GetCurrentApplicationVersion()
        {
            return this.currentApplicationVersion ?? new ApplicationVersion(0, 0, 0);
        }

        public ApplicationVersion GetNewestAvailableApplicationVersion()
        {
            try
            {
                if (this.newestAvailableApplicationVersion == null)
                {
                    var versionFromGitHub = this.GetNewestReleaseNameFromGitHubRepositoryUsingRestApi();
                    this.newestAvailableApplicationVersion = new ApplicationVersion(versionFromGitHub);
                }

                return this.newestAvailableApplicationVersion;
            } catch
            {
                return this.GetCurrentApplicationVersion();
            }
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
                    UseShellExecute = false, // Set to true to allow batch script execution
                    WorkingDirectory = appDirectory // Ensure it's executed in the app's directory
                };

                Process.Start(startInfo);

                Process.GetCurrentProcess().Kill();
            }
            else
            {
                throw new ArgumentNullException("ApplicationUpdater", "Updater Applicatoin was not found.");
            }
        }

        private string GetNewestReleaseNameFromGitHubRepositoryUsingRestApi()
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

                return result.Where(v => v.prerelease == false).OrderByDescending(v =>v.published_at).FirstOrDefault().name;
            }

            throw new Exception("Cannot get newest release name from GitHub repository.");
        }
    }
}
