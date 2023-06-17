using MediatR;
using TrackYourDay.Core.Events;

namespace TrackYourDay.Tests
{
    internal class SystemEventRecognizedPersistenceHandler : INotificationHandler<SystemEventRecognizedNotification>
    {
        private readonly SystemEventRepository systemEventRepository;

        public SystemEventRecognizedPersistenceHandler()
        {
            this.systemEventRepository = new SystemEventRepository();
        }
        public Task Handle(SystemEventRecognizedNotification notification, CancellationToken cancellationToken)
        {
            // Place for inbox pattern
            this.systemEventRepository.SaveEvent(notification.SystemEvent);
            return Task.CompletedTask;
        }
    }
}