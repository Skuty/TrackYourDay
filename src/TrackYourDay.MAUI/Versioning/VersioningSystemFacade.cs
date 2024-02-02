using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Reflection;

namespace TrackYourDay.MAUI.Versioning
{
    public class VersioningSystemFacade
    {
        private ApplicationVersion newestAvailableApplicationVersion = null!;

        public ApplicationVersion GetCurrentApplicationVersion()
        {
            return new ApplicationVersion(Assembly.GetExecutingAssembly().GetName().Version);
        }

        public ApplicationVersion GetNewestAvailableApplicationVersion()
        {
            try
            {
                if (newestAvailableApplicationVersion == null)
                {
                    var versionFromGitHub = GetNewestReleaseNameFromGitHubRepositoryUsingRestApi();
                    newestAvailableApplicationVersion = new ApplicationVersion(versionFromGitHub);
                }

                return newestAvailableApplicationVersion;
            }
            catch
            {
                return GetCurrentApplicationVersion();
            }
        }

        public bool IsNewerVersionAvailable()
        {
            return GetNewestAvailableApplicationVersion().IsNewerThan(GetCurrentApplicationVersion());
        }

        private string GetNewestReleaseNameFromGitHubRepositoryUsingRestApi()
        {
            var url = "https://api.github.com/repos/skuty/TrackYourDay/releases";

            // TODO: Replace with injected HttpClient from IHttpClientFactory
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(url);
            var productValue = new ProductInfoHeaderValue("TrackYourDay", GetCurrentApplicationVersion().ToString());

            client.DefaultRequestHeaders.UserAgent.Add(productValue);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

            HttpResponseMessage response = client.GetAsync(url).Result;


            if (response.IsSuccessStatusCode)
            {
                var json = response.Content.ReadAsStringAsync().Result;
                var result = JsonConvert.DeserializeObject<List<GitHubReleaseResponse>>(json);

                return result.Where(v => v.prerelease == false).OrderByDescending(v => v.published_at).FirstOrDefault().name;
            }

            throw new Exception("Cannot get newest release name from GitHub repository.");
        }
    }
}
