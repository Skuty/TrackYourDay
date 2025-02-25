using MediatR;
using Microsoft.UI;
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
                        overlappedPresenter.SetBorderAndTitleBar(false, false);
                        window.Height -= HeaderHeightInPx;
                    }
                    else
                    {
                        overlappedPresenter.SetBorderAndTitleBar(true, true);
                        window.Height += HeaderHeightInPx;
                    }
                    overlappedPresenter.Restore();
                    break;
            }

            return Task.CompletedTask;
        }
    }
}
