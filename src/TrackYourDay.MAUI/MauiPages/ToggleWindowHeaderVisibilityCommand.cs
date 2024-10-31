using MediatR;
using Microsoft.UI;
using WinRT.Interop;

namespace TrackYourDay.MAUI.MauiPages
{
    internal class ToggleWindowHeaderVisibilityCommand : IRequest
    {
        public Guid WindowId { get; }

        public ToggleWindowHeaderVisibilityCommand(Guid windowId)
        {
            this.WindowId = windowId;
        }
    }

    internal class ToggleWindowHeaderVisibilityCommandHandler : IRequestHandler<ToggleWindowHeaderVisibilityCommand>
    {
        public Task Handle(ToggleWindowHeaderVisibilityCommand request, CancellationToken cancellationToken)
        {
            var windowToClose = Application.Current?.Windows.FirstOrDefault(w =>
                w.Id == request.WindowId || w.Page.Id == request.WindowId);

            var localWindow = (windowToClose.GetVisualElementWindow().Handler.PlatformView as Microsoft.UI.Xaml.Window);

            localWindow.ExtendsContentIntoTitleBar = false;
            var handle = WindowNative.GetWindowHandle(localWindow);
            var id = Win32Interop.GetWindowIdFromWindow(handle);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(id);

            switch (appWindow.Presenter)
            {
                case Microsoft.UI.Windowing.OverlappedPresenter overlappedPresenter:
                    overlappedPresenter.IsAlwaysOnTop = true;

                    if (overlappedPresenter.HasBorder)
                    {
                        overlappedPresenter.SetBorderAndTitleBar(false, false);
                    }
                    else
                    {
                        overlappedPresenter.SetBorderAndTitleBar(true, true);
                    }
                    overlappedPresenter.Restore();
                    break;
            }

            return Task.CompletedTask;
        }
    }
}
