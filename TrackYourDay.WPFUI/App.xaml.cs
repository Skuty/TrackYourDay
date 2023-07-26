using Microsoft.Extensions.DependencyInjection;
using Quartz;
using System;
using System.Threading.Tasks;
using System.Windows;
using TrackYourDay.Core;
using TrackYourDay.Core.Old.Activities;
using TrackYourDay.Core.Old.Activities.RecognizingStrategies;
using TrackYourDay.Core.Old.Breaks;
using TrackYourDay.WPFUI.BackgroundJobs;

namespace TrackYourDay.WPFUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IServiceProvider serviceProvider { get; set; } = null!;

        protected override async void OnStartup(StartupEventArgs eventArgs)
        {
            this.ConfigureDependencyInjection();
            await this.ScheduleJobs();
        }

        private void ConfigureServices(ServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<ISharedInstance, SharedInstance>();

            serviceCollection.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<ActivityEventTracker>());
            serviceCollection.AddSingleton<IClock, Clock>();
            serviceCollection.AddScoped<IActivityRecognizingStrategy, WindowNameActivityRecognizingStrategy>();
            serviceCollection.AddSingleton<ActivityEventTracker>();
            serviceCollection.AddSingleton<BreakTracker>();

            serviceCollection.AddQuartz(o => o.UseMicrosoftDependencyInjectionJobFactory());
            serviceCollection.AddQuartzHostedService();

            serviceCollection.AddWpfBlazorWebView();
        }

        private async Task ScheduleJobs()
        {
            var schedulerFactory = this.serviceProvider.GetRequiredService<ISchedulerFactory>();
            var scheduler = await schedulerFactory.GetScheduler();

            await scheduler.ScheduleJob(ActivityEventTrackerJob.DefaultJobDetail,
                                        ActivityEventTrackerJob.DefaultTrigger);
            await scheduler.Start();
        }

        private void ConfigureDependencyInjection()
        {
            var serviceCollection = new ServiceCollection();
            this.ConfigureServices(serviceCollection);
            this.serviceProvider = serviceCollection.BuildServiceProvider();
            Resources.Add("services", this.serviceProvider);
        }
    }
}
