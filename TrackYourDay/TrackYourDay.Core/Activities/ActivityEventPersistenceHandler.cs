using MediatR;

namespace TrackYourDay.Core.Activities
{
    internal class ActivityEventPersistenceHandler : INotificationHandler<ActivityEventRecognizedNotification>
    {
        private readonly ActivityEventRepository systemEventRepository;

        public ActivityEventPersistenceHandler()
        {
            systemEventRepository = new ActivityEventRepository();
        }
        public Task Handle(ActivityEventRecognizedNotification notification, CancellationToken cancellationToken)
        {
            // Place for inbox pattern
            systemEventRepository.SaveEvent(notification.SystemEvent);
            return Task.CompletedTask;
        }
    }
}