using Quartz;
using TrackYourDay.Core.ApplicationTrackers.MsTeams;

namespace TrackYourDay.MAUI.BackgroundJobs.ActivityTracking;

/// <summary>
/// Background job that polls for MS Teams meeting window changes.
/// DisallowConcurrentExecution prevents race conditions in singleton tracker state.
/// </summary>
[DisallowConcurrentExecution]
internal class MsTeamsMeetingsTrackerJob : IJob
{
    private readonly MsTeamsMeetingTracker _tracker;

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

    public MsTeamsMeetingsTrackerJob(MsTeamsMeetingTracker tracker)
    {
        _tracker = tracker;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await _tracker.RecognizeActivityAsync().ConfigureAwait(false);
    }
}
