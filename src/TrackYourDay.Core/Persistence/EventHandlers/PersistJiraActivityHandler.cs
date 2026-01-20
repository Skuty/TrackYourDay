using MediatR;
using Microsoft.Extensions.Logging;
using TrackYourDay.Core.ApplicationTrackers.Jira;
using TrackYourDay.Core.ApplicationTrackers.Jira.PublicEvents;

namespace TrackYourDay.Core.Persistence.EventHandlers
{
    /// <summary>
    /// Handles JiraActivityDiscoveredEvent by persisting the discovered activity to the database.
    /// </summary>
    public class PersistJiraActivityHandler : INotificationHandler<JiraActivityDiscoveredEvent>
    {
        private readonly IHistoricalDataRepository<JiraActivity> repository;
        private readonly ILogger<PersistJiraActivityHandler> logger;

        public PersistJiraActivityHandler(
            IHistoricalDataRepository<JiraActivity> repository,
            ILogger<PersistJiraActivityHandler> logger)
        {
            this.repository = repository;
            this.logger = logger;
        }

        public Task Handle(JiraActivityDiscoveredEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                repository.Save(notification.Activity);
                logger.LogDebug("Persisted Jira activity with Guid: {ActivityGuid}", notification.Activity.Guid);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to persist Jira activity with Guid: {ActivityGuid}", notification.Activity.Guid);
                throw;
            }

            return Task.CompletedTask;
        }
    }
}
