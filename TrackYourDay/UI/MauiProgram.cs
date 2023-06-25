using Hangfire;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using TrackYourDay.Core;
using TrackYourDay.Core.Activities;
using TrackYourDay.Core.Breaks;
using UI.Data;

namespace UI;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMudServices();
        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        builder.Services.AddSingleton<WeatherForecastService>();

        builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<ActivityEventTracker>());

        builder.Services.AddSingleton<IClock, Clock>();
        builder.Services.AddSingleton<ActivityEventTracker>();
        builder.Services.AddSingleton<BreakTracker>();

        builder.Services.AddHangfire(c => c.UseInMemoryStorage());
        builder.Services.AddHangfireServer();

        var buildedBuilder = builder.Build();
        var activityEventTracker = buildedBuilder.Services.GetRequiredService<ActivityEventTracker>();
        RecurringJob.AddOrUpdate("ActivityTrackerJob", () => activityEventTracker.RecognizeEvents(), Cron.Minutely);
        return buildedBuilder;
    }
}
