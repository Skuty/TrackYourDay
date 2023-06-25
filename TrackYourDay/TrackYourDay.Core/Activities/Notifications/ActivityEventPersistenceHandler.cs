using MediatR;
using TrackYourDay.Core.Activities.Repositories;

namespace TrackYourDay.Core.Activities.Notifications
{
    internal class ActivityEventPersistenceHandler : INotificationHandler<ActivityEventRecognizedNotification>
    {
        private readonly ActivityEventInMemoryRepository systemEventRepository;

        public ActivityEventPersistenceHandler()
        {
            systemEventRepository = new ActivityEventInMemoryRepository();
        }
        public Task Handle(ActivityEventRecognizedNotification notification, CancellationToken cancellationToken)
        {
            // Place for inbox pattern
            systemEventRepository.SaveEvent(notification.ActivityEvent);
            return Task.CompletedTask;
        }
    }
}