using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using System.Reflection;
using TrackYourDay.Core.ServiceRegistration;
using TrackYourDay.Core.Activities;
using static MudBlazor.CategoryTypes;
using TrackYourDay.Core.Settings;

namespace TrackYourDay.Web
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");
            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

            builder.Services.AddMudServices();

#if DEBUG
            builder.Services.AddSingleton(Assembly.GetExecutingAssembly().GetName().Version);

            builder.Services.AddSingleton<ISettingsRepository, InMemorySettingsRepository>();

            builder.Services.AddSettings();

            builder.Services.AddTrackers();

            builder.Services.AddCoreNotifications();

            builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<ActivityTracker>());


#endif

            await builder.Build().RunAsync();
        }
    }
}
