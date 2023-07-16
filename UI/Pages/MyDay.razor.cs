using UI.Core;

namespace UI.Pages
{
    public partial class MyDay
    {
        IList<MyActivity> myActivities = new List<MyActivity>();
        MyActivitiesService myActivitiesService = new MyActivitiesService();
        SystemStateTracker activityTracker = new SystemStateTracker();

        private bool loading;
        private bool tracking;

        protected override void OnInitialized()
        {
            base.OnInitialized();
            this.Refresh();
        }

        private void Refresh()
        {
            this.loading = true;
            this.myActivities = myActivitiesService.GetActivities();
            this.loading = false;
        }

        private void RecordState()
        {
            this.myActivitiesService.Subscribe(activityTracker);
            this.activityTracker.PublishSystemState();
            this.myActivitiesService.Unsubscribe();
        }

        private async Task ToggleTrackingAsync()
        {
            if (this.tracking)
            {
                this.tracking = false;
                this.myActivitiesService.Unsubscribe();
            }
            else
            {
                this.tracking = true;
                this.myActivitiesService.Subscribe(activityTracker);

                var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
                while (await timer.WaitForNextTickAsync())
                {
                    this.activityTracker.PublishSystemState();
                }
            }
        }
    }
}
