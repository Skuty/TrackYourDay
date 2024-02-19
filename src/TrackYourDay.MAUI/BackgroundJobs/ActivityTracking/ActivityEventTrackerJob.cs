using Quartz;
using System.Threading.Tasks;
using TrackYourDay.Core.Activities;

namespace TrackYourDay.MAUI.BackgroundJobs.ActivityTracking
{
    internal class ActivityEventTrackerJob : IJob
    {
        private readonly ActivityTracker activityTracker;

        internal static IJobDetail DefaultJobDetail => JobBuilder.Create<ActivityEventTrackerJob>()
            .WithIdentity("ActivityEventTracker", "Trackers")
            .Build();

        internal static ITrigger DefaultTrigger => TriggerBuilder.Create()
             .WithIdentity("ActivityEventTracker", "DefaultGroup")
             .StartNow()
             .WithSimpleSchedule(x => x
                  .WithIntervalInSeconds(10)
                  .RepeatForever())
             .Build();

        public ActivityEventTrackerJob(ActivityTracker activityTracker)
        {
            this.activityTracker = activityTracker;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            activityTracker.RecognizeActivity();
        }
    }
}
