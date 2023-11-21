using Newtonsoft.Json;
using System.Net.Http.Headers;

namespace TrackYourDay.Core.Versioning
{
    public class VersioningSystemFacade
    {
        private ApplicationVersion newestAvailableApplicationVersion = null!;

        public ApplicationVersion GetCurrentApplicationVersion()
        {
            return new ApplicationVersion("0.1.4");
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

                return result.OrderByDescending(v =>v.published_at).FirstOrDefault().name;
            }

            throw new Exception("Cannot get newest release name from GitHub repository.");
        }
    }
}
