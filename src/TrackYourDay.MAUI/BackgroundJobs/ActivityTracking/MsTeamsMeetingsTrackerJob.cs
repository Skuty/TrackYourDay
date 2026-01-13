using Quartz;
using TrackYourDay.Core.ApplicationTrackers.MsTeams;

namespace TrackYourDay.MAUI.BackgroundJobs.ActivityTracking
{
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

        public Task Execute(IJobExecutionContext context)
        {
            _tracker.RecognizeActivity();
            return Task.CompletedTask;
        }
    }
}
