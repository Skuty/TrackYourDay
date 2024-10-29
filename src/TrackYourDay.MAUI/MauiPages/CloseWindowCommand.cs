using MediatR;

namespace TrackYourDay.MAUI.MauiPages
{
    internal class CloseWindowCommand : IRequest
    {
        public Guid MauiWindowId { get; }

        public CloseWindowCommand(Guid mauiWindowId)
        {
            this.MauiWindowId = mauiWindowId;
        }
    }

    internal class CloseWindowCommandHandler : IRequestHandler<CloseWindowCommand>
    {
        public Task Handle(CloseWindowCommand request, CancellationToken cancellationToken)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            { 
                try
                {
                    var windowToClose = Application.Current?.Windows.FirstOrDefault(w =>
                        w.Id == request.MauiWindowId || w.Page.Id == request.MauiWindowId);
                    Application.Current?.CloseWindow(windowToClose);
                } catch (Exception ex) { };
            });

            return Task.CompletedTask;
        }
    }
}
