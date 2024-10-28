using MediatR;

namespace TrackYourDay.MAUI.MauiPages
{
    internal class CloseWindowCommand : IRequest
    {
        public Type PageType { get; }

        public int PageHashCode { get; }

        public CloseWindowCommand(Type pageType, int pageHashCode)
        {
            PageType = pageType;
            PageHashCode = pageHashCode;
        }
    }

    internal class CloseWindowCommandHandler : IRequestHandler<CloseWindowCommand>
    {
        public Task Handle(CloseWindowCommand request, CancellationToken cancellationToken)
        {
            var windowToClose = Application.Current?.Windows.FirstOrDefault(w => 
                w.Page.GetType() == request.PageType
                && w.Page.GetHashCode() == request.PageHashCode);
            Application.Current?.CloseWindow(windowToClose);

            return Task.CompletedTask;
        }
    }
}
