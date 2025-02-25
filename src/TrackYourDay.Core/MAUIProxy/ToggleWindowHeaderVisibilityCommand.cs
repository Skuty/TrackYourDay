using MediatR;

namespace TrackYourDay.Core.MAUIProxy
{
    public class ToggleWindowHeaderVisibilityCommand : IRequest
    {
        public Guid WindowId { get; }

        public ToggleWindowHeaderVisibilityCommand(Guid windowId)
        {
            this.WindowId = windowId;
        }
    }
}
