using MediatR;
using Microsoft.Extensions.Logging;
using TrackYourDay.Core.ApplicationTrackers.GitLab;
using TrackYourDay.Core.ApplicationTrackers.GitLab.PublicEvents;

namespace TrackYourDay.Core.Persistence.EventHandlers
{
    /// <summary>
    /// Handles GitLabActivityDiscoveredEvent by persisting the discovered activity to the database.
    /// </summary>
    public class PersistGitLabActivityHandler : INotificationHandler<GitLabActivityDiscoveredEvent>
    {
        private readonly IHistoricalDataRepository<DiscoveredGitLabActivity> repository;
        private readonly ILogger<PersistGitLabActivityHandler> logger;

        public PersistGitLabActivityHandler(
            IHistoricalDataRepository<DiscoveredGitLabActivity> repository,
            ILogger<PersistGitLabActivityHandler> logger)
        {
            this.repository = repository;
            this.logger = logger;
        }

        public Task Handle(GitLabActivityDiscoveredEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                var discoveredActivity = new DiscoveredGitLabActivity(
                    notification.Guid,
                    notification.Activity.OccuranceDate,
                    notification.Activity.Description);

                repository.Save(discoveredActivity);
                logger.LogDebug("Persisted GitLab activity with Guid: {ActivityGuid}", discoveredActivity.Guid);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to persist GitLab activity with Guid: {ActivityGuid}", notification.Guid);
                throw;
            }

            return Task.CompletedTask;
        }
    }
}
