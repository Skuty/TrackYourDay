using MediatR;
using TrackYourDay.Tests.Old.Old.Activities.Repositories;

namespace TrackYourDay.Tests.Old.Old.Activities.Notifications
{
    internal class ActivityEventPersistenceHandler : INotificationHandler<ActivityEventRecognizedEvent>
    {
        private readonly ActivityEventInMemoryRepository systemEventRepository;

        public ActivityEventPersistenceHandler()
        {
            systemEventRepository = new ActivityEventInMemoryRepository();
        }

        public Task Handle(ActivityEventRecognizedEvent _event, CancellationToken cancellationToken)
        {
            // Place for inbox pattern
            systemEventRepository.SaveEvent(_event.ActivityEvent);
            return Task.CompletedTask;
        }
    }
}