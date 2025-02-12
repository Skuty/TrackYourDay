using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using System.Reflection;
using TrackYourDay.Core.ServiceRegistration;
using TrackYourDay.Core.Activities;
using static MudBlazor.CategoryTypes;

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

            builder.Services.AddSingleton(Assembly.GetExecutingAssembly().GetName().Version);

            builder.Services.AddSettings();

            builder.Services.AddTrackers();

            builder.Services.AddCoreNotifications();

            builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<ActivityTracker>());

            await builder.Build().RunAsync();
        }
    }
}
