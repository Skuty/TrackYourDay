using Hangfire;
using Quartz;
using System;
using System.Threading.Tasks;

namespace TrackYourDay.WPFUI.BackgroundJobs
{
    internal class ActivityEventTrackerJob : IJob
    {
        private readonly ISharedInstance sharedInstance;

        internal static IJobDetail DefaultJobDetail => JobBuilder.Create<ActivityEventTrackerJob>()
            .WithIdentity("ActivityEventTracker", "Trackers")
            .Build();

        internal static ITrigger DefaultTrigger => TriggerBuilder.Create()
             .WithIdentity("ActivityEventTracker", "DefaultGroup")
             .StartNow()
             .WithSimpleSchedule(x => x
                  .WithIntervalInSeconds(3)
                  .RepeatForever())
             .Build();

        public ActivityEventTrackerJob(ISharedInstance sharedInstance)
        {
            this.sharedInstance = sharedInstance;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            this.sharedInstance.Increment();
            await Console.Out.WriteLineAsync("ActivityEventTrackerJob");
        }
    }
}
