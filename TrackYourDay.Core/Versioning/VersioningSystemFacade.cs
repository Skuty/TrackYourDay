using Newtonsoft.Json;
using System.Net.Http.Headers;

namespace TrackYourDay.Core.Versioning
{
    public class VersioningSystemFacade
    {
        private ApplicationVersion newestAvailableApplicationVersion;

        public ApplicationVersion GetCurrentApplicationVersion()
        {
            return new ApplicationVersion("0.0.1");
        }

        public ApplicationVersion GetNewestAvailableApplicationVersion()
        {
            if (this.newestAvailableApplicationVersion == null)
            {
                var versionFromGitHub = this.GetNewestReleaseNameFromGitHubRepositoryUsingRestApi();
                this.newestAvailableApplicationVersion = new ApplicationVersion(versionFromGitHub);
            }

            return this.newestAvailableApplicationVersion;
        }

        public bool IsNewerVersionAvailable()
        {
            return this.GetCurrentApplicationVersion().IsNewerThan(this.GetNewestAvailableApplicationVersion());
        }

        private string GetNewestReleaseNameFromGitHubRepositoryUsingRestApi()
        {
            var url = "https://api.github.com/repos/skuty/TrackYourDay/releases";

            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(url);
            var productValue = new ProductInfoHeaderValue("TrackYourDay", "0.1");

            client.DefaultRequestHeaders.UserAgent.Add(productValue);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            HttpResponseMessage response = client.GetAsync(url).Result;

            //if (response.IsSuccessStatusCode)
            //{
                var result = response.Content.ReadAsStringAsync().Result;
                dynamic obj = JsonConvert.DeserializeObject<dynamic>(result);

                return obj.First().name;
            //}

            throw new Exception("Cannot get newest release name from GitHub repository.");
        }
    }
}
