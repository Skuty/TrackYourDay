using MediatR;

namespace TrackYourDay.Core.MAUIProxy
{
    public class CloseWindowCommand : IRequest
    {
        public Guid MauiWindowId { get; }

        public CloseWindowCommand(Guid mauiWindowId)
        {
            this.MauiWindowId = mauiWindowId;
        }
    }
}
