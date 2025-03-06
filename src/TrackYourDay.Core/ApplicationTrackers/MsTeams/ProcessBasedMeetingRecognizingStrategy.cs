using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace TrackYourDay.Core.ApplicationTrackers.MsTeams
{
    public class ProcessBasedMeetingRecognizingStrategy : IMeetingDiscoveryStrategy
    {
        private readonly ILogger<ProcessBasedMeetingRecognizingStrategy> logger;
        private readonly IProcessService processService;

        public ProcessBasedMeetingRecognizingStrategy(
            ILogger<ProcessBasedMeetingRecognizingStrategy> logger,
            IProcessService processService)
        {
            this.logger = logger;
            this.processService = processService;
        }

        public StartedMeeting RecognizeMeeting()
        {
            var teamsProcesses = processService.GetProcesses()
                .Where(p => p.ProcessName.Contains("ms-teams", StringComparison.InvariantCulture));

            foreach (var process in teamsProcesses)
            {
                logger.LogInformation("Found Teams process! Name: {0}, Title: {1}",
                    process.ProcessName, process.MainWindowTitle);
            }

            foreach (var process in teamsProcesses)
            {
                if (this.IsWindowTitleMatchingTeamsMeeting(process.MainWindowTitle))
                {
                    return new StartedMeeting(Guid.NewGuid(), DateTime.Now, process.MainWindowTitle);
                }
            }

            return null;
        }

        private bool IsWindowTitleMatchingTeamsMeeting(string windowTitle)
        {
            if (string.IsNullOrEmpty(windowTitle))
                return false;

            if (!windowTitle.Contains("Microsoft Teams", StringComparison.InvariantCultureIgnoreCase))
                return false;

            // Exclude known non-meeting windows
            if (windowTitle.StartsWith("Czat |", StringComparison.InvariantCultureIgnoreCase) ||
                windowTitle.StartsWith("Aktywność |", StringComparison.InvariantCultureIgnoreCase) ||
                windowTitle == "Microsoft Teams")
                return false;

            return true;
        }
    }
}
