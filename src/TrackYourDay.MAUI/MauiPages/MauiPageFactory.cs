using MediatR;
using Microsoft.UI;
using Microsoft.UI.Windowing;
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
        public static void OpenWebPageInNewWindow(string path, int width, int height)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Window blazorPopUpPage = new Window(new PopupBlazorPage(path));
                blazorPopUpPage.Y = 0;
                blazorPopUpPage.X = 0;
                blazorPopUpPage.Title = $"Track Your Day - Pop Up";

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

                        overlappedPresenter.SetBorderAndTitleBar(false, false);
                        overlappedPresenter.Restore();
                        break;
                }
                
                // Have to be setted last otherwise Window Header will set its height always to its defualt height
                // There won't be possibility to change height lower than height of top border
                blazorPopUpPage.Width = width;
                blazorPopUpPage.Height = height;
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
