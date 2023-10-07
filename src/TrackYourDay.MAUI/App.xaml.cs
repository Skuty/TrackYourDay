using Microsoft.Extensions.Logging;
using Quartz;

namespace TrackYourDay.MAUI
{
    public partial class App : Application
    {
        private readonly ISchedulerFactory schedulerFactory;
        public App(ISchedulerFactory schedulerFactory, ILogger<App> logger)
        {
            InitializeComponent();
            MainPage = new MainPage();

            this.schedulerFactory = schedulerFactory;
        }

        protected override void OnStart()
        {
            base.OnStart();

            // Scheduler have to be started manually due to lack of full support for HostedServices in MAUI
            var sched = this.schedulerFactory.GetScheduler().Result;
            sched.Start();
        }
    }
}