using MediatR;
using Microsoft.UI;
using WinRT.Interop;

namespace TrackYourDay.MAUI.MauiPages
{
    internal class MinimizeWindowCommand : IRequest
    {
        public Guid WindowId { get; }

        public MinimizeWindowCommand(Guid windowId)
        {
            this.WindowId = windowId;
        }
    }

    internal class MinimizeWindowCommandHandler : IRequestHandler<MinimizeWindowCommand>
    {
        public Task Handle(MinimizeWindowCommand request, CancellationToken cancellationToken)
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
                    overlappedPresenter.Minimize();
                    break;
            }

            return Task.CompletedTask;
        }
    }
}
