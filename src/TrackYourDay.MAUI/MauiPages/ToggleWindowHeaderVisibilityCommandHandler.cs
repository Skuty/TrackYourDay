using MediatR;
using Microsoft.UI;
using TrackYourDay.Core.MAUIProxy;
using WinRT.Interop;

namespace TrackYourDay.MAUI.MauiPages
{
    internal class ToggleWindowHeaderVisibilityCommandHandler : IRequestHandler<ToggleWindowHeaderVisibilityCommand>
    {
        private const int HeaderHeightInPx = 40;
        public Task Handle(ToggleWindowHeaderVisibilityCommand request, CancellationToken cancellationToken)
        {
            var window = Application.Current?.Windows.FirstOrDefault(w =>
                w.Id == request.WindowId || w.Page.Id == request.WindowId);
            var currentWidth = window.Width;
            var currentHeight = window.Height;

            var visualElement = (window.GetVisualElementWindow().Handler.PlatformView as Microsoft.UI.Xaml.Window);

            visualElement.ExtendsContentIntoTitleBar = false;
            var handle = WindowNative.GetWindowHandle(visualElement);
            var id = Win32Interop.GetWindowIdFromWindow(handle);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(id);

            switch (appWindow.Presenter)
            {
                case Microsoft.UI.Windowing.OverlappedPresenter overlappedPresenter:
                    overlappedPresenter.IsAlwaysOnTop = true;

                    if (overlappedPresenter.HasBorder)
                    {
                        overlappedPresenter.SetBorderAndTitleBar(true, false);
                        window.Height -= HeaderHeightInPx;
                        //window.Width = currentWidth;
                    }
                    else
                    {
                        overlappedPresenter.SetBorderAndTitleBar(true, true);
                        window.Height += HeaderHeightInPx;
                        //window.Width = currentWidth;

                    }
                    overlappedPresenter.Restore();
                    break;
            }

            return Task.CompletedTask;
        }
    }
}
