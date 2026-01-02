using Microsoft.Extensions.Logging;
using Quartz;
using TrackYourDay.MAUI.MauiPages;

namespace TrackYourDay.MAUI
{
    public partial class App : Application
    {
        private readonly ISchedulerFactory schedulerFactory;
        public App(ISchedulerFactory schedulerFactory, TrackYourDay.Core.LlmPrompts.ITemplateManagementService templateManagementService, ILogger<App> logger)
        {
            InitializeComponent();
            MainPage = new MainPage();

            this.schedulerFactory = schedulerFactory;
            templateManagementService.SeedDefaultTemplates();
        }

        protected override void OnStart()
        {
            base.OnStart();

            // Scheduler have to be started manually due to lack of full support for HostedServices in MAUI
            var sched = this.schedulerFactory.GetScheduler().Result;
            sched.Start();

            this.MinimizeWindowOnCloseInsteadOfClosing();

            MauiPageFactory.OpenWebPageInNewWindow("/OperationalBar", 550, 30);
        }

        private void MinimizeWindowOnCloseInsteadOfClosing()
        {
            var window = Application.Current?.Windows.FirstOrDefault()
                .GetVisualElementWindow().Handler.PlatformView as Microsoft.UI.Xaml.Window;

            window.ExtendsContentIntoTitleBar = false;
            var handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
            var id = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(handle);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(id);

            switch (appWindow.Presenter)
            {
                case Microsoft.UI.Windowing.OverlappedPresenter overlappedPresenter:
                    //disable the max button
                    overlappedPresenter.IsMaximizable = true;
                    overlappedPresenter.Maximize();
                    break;
            }

            //When user execute the closing method, we can make the window do not close by   e.Cancel = true;.
            appWindow.Closing += async (s, e) =>
            {
                e.Cancel = true;
                switch (appWindow.Presenter)
                {
                    case Microsoft.UI.Windowing.OverlappedPresenter overlappedPresenter:
                        overlappedPresenter.Minimize();
                        break;
                }
            };
        }
    }
}