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
        private readonly IHistoricalDataRepository<GitLabActivity> repository;
        private readonly ILogger<PersistGitLabActivityHandler> logger;

        public PersistGitLabActivityHandler(
            IHistoricalDataRepository<GitLabActivity> repository,
            ILogger<PersistGitLabActivityHandler> logger)
        {
            this.repository = repository;
            this.logger = logger;
        }

        public Task Handle(GitLabActivityDiscoveredEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                repository.Save(notification.Activity);
                logger.LogDebug("Persisted GitLab activity with Guid: {ActivityGuid}", notification.Activity.Guid);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to persist GitLab activity with Guid: {ActivityGuid}", notification.Activity.Guid);
                throw;
            }

            return Task.CompletedTask;
        }
    }
}
