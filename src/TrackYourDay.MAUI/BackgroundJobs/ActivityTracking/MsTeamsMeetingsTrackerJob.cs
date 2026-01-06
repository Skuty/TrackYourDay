using Quartz;
using Microsoft.Extensions.DependencyInjection;
using TrackYourDay.Core.ApplicationTrackers.MsTeams;

namespace TrackYourDay.MAUI.BackgroundJobs.ActivityTracking
{
    internal class MsTeamsMeetingsTrackerJob : IJob
    {
        private readonly IServiceScopeFactory _scopeFactory;

        internal static IJobDetail DefaultJobDetail => JobBuilder.Create<MsTeamsMeetingsTrackerJob>()
            .WithIdentity("MsTeamsMeetingsTracker", "Trackers")
            .Build();

        internal static ITrigger DefaultTrigger => TriggerBuilder.Create()
             .WithIdentity("MsTeamsMeetingsTracker", "DefaultGroup")
             .StartNow()
             .WithSimpleSchedule(x => x
                  .WithIntervalInSeconds(10)
                  .RepeatForever())
             .Build();

        public MsTeamsMeetingsTrackerJob(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            using var scope = _scopeFactory.CreateScope();
            var tracker = scope.ServiceProvider.GetRequiredService<MsTeamsMeetingTracker>();
            tracker.RecognizeActivity();
        }
    }
}
