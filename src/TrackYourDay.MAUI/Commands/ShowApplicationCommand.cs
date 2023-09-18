using Microsoft.UI.Windowing;
using Microsoft.UI;
using System;
using System.Windows;
using System.Windows.Input;

namespace TrackYourDay.MAUI.Commands
{
    internal class ShowApplicationCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            var Window = App.Current.Windows.First();
            var nativeWindow = Window.Handler.PlatformView;
            IntPtr windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(nativeWindow);
            WindowId WindowId = Win32Interop.GetWindowIdFromWindow(windowHandle);
            AppWindow appWindow = AppWindow.GetFromWindowId(WindowId);

            var p = appWindow.Presenter as OverlappedPresenter;

            p.Maximize();
        }
    }
}
