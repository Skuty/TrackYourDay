using MediatR;

namespace TrackYourDay.Core.MAUIProxy
{
    public class MinimizeWindowCommand : IRequest
    {
        public Guid WindowId { get; }

        public MinimizeWindowCommand(Guid windowId)
        {
            this.WindowId = windowId;
        }
    }
}
