using MediatR;
using Microsoft.UI;
using WinRT.Interop;

namespace TrackYourDay.MAUI.MauiPages
{
    public class MauiPageFactory
    {
        private readonly IMediator mediator;

        internal MauiPageFactory(IMediator mediator)
        {
            this.mediator = mediator;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        public static void OpenWebPageInNewWindow(string path) 
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Window blazorPopUpPage = new Window(new PopupBlazorPage(path));
                blazorPopUpPage.Title = $"Track Your Day - Pop Up";
                blazorPopUpPage.Width = 385;
                blazorPopUpPage.Height = 215;
                Application.Current.OpenWindow(blazorPopUpPage);

                var localWindow = (blazorPopUpPage.Handler.PlatformView as Microsoft.UI.Xaml.Window);

                localWindow.ExtendsContentIntoTitleBar = false;
                var handle = WindowNative.GetWindowHandle(localWindow);
                var id = Win32Interop.GetWindowIdFromWindow(handle);
                var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(id);

                switch (appWindow.Presenter)
                {
                    case Microsoft.UI.Windowing.OverlappedPresenter overlappedPresenter:
                        overlappedPresenter.IsResizable = false;
                        overlappedPresenter.IsMaximizable = false;
                        overlappedPresenter.IsMinimizable = false;
                        overlappedPresenter.IsAlwaysOnTop = true;

                        //overlappedPresenter.SetBorderAndTitleBar(true, false);
                        overlappedPresenter.Restore();
                        break;
                }
            });
        }

        public void OpenSimpleNotificationPageInNewWindow(SimpleNotificationViewModel simpleNotificationViewModel)
        {
            // https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/appmodel/main-thread?view=net-maui-8.0
            // Needed to show notification on main thread otherwise it will throw exception
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Window simpleNotificationPage = new Window(new SimpleNotificationPage(simpleNotificationViewModel, this.mediator));
                simpleNotificationPage.Title = $"Track Your Day - {simpleNotificationViewModel.Title}";
                simpleNotificationPage.Width = 600;
                simpleNotificationPage.Height = 170;
                Application.Current.OpenWindow(simpleNotificationPage);

                var localWindow = (simpleNotificationPage.Handler.PlatformView as Microsoft.UI.Xaml.Window);

                localWindow.ExtendsContentIntoTitleBar = false;
                var handle = WindowNative.GetWindowHandle(localWindow);
                var id = Win32Interop.GetWindowIdFromWindow(handle);
                var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(id);

                switch (appWindow.Presenter)
                {
                    case Microsoft.UI.Windowing.OverlappedPresenter overlappedPresenter:
                        overlappedPresenter.IsResizable = false;
                        overlappedPresenter.IsMaximizable = false;
                        overlappedPresenter.IsMinimizable = false;
                        overlappedPresenter.IsAlwaysOnTop = true;

                        //overlappedPresenter.SetBorderAndTitleBar(true, false);
                        overlappedPresenter.Restore();
                        break;
                }
            });
        }

    }
}
