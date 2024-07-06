using Microsoft.UI;
using WinRT.Interop;

namespace TrackYourDay.MAUI.MauiPages
{
    internal class MauiPageFactory
    {
        private MauiPageFactory()
        {
        }

        public static void OpenSimpleNotificationPageInNewWindow(SimpleNotificationViewModel simpleNotificationViewModel)
        {
            // https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/appmodel/main-thread?view=net-maui-8.0
            // Needed to show notification on main thread otherwise it will throw exception
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Window breakRevokingPopupWindow = new Window(new SimpleNotificationPage(simpleNotificationViewModel));
                breakRevokingPopupWindow.Title = $"Track Your Day - {simpleNotificationViewModel.Title}";
                breakRevokingPopupWindow.Width = 600;
                breakRevokingPopupWindow.Height = 170;
                Application.Current.OpenWindow(breakRevokingPopupWindow);

                var localWindow = (breakRevokingPopupWindow.Handler.PlatformView as Microsoft.UI.Xaml.Window);

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
