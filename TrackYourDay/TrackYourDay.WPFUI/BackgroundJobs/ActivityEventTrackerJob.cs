using Hangfire;
using Quartz;
using System;
using System.Threading.Tasks;
using TrackYourDay.Core.Activities;

namespace TrackYourDay.WPFUI.BackgroundJobs
{
    internal class ActivityEventTrackerJob : IJob
    {
        private readonly ActivityEventTracker activityEventTracker;

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

        public ActivityEventTrackerJob(ActivityEventTracker activityEventTracker)
        {
            this.activityEventTracker = activityEventTracker;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            this.activityEventTracker.RecognizeEvents();
        }
    }
}
