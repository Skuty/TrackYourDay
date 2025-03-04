using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace TrackYourDay.Core.ApplicationTrackers.MsTeams
{
    public class ProcessBasedMeetingRecognizingStrategy : IMeetingDiscoveryStrategy
    {
        private ILogger<ProcessBasedMeetingRecognizingStrategy> logger;

        public ProcessBasedMeetingRecognizingStrategy(ILogger<ProcessBasedMeetingRecognizingStrategy> logger)
        {
            this.logger = logger;
        }

        public StartedMeeting RecognizeMeeting()
        {
            var teamsProcess = Process.GetProcesses()
                .Where(p => p.ProcessName.Contains("teams", StringComparison.InvariantCulture));
            this.logger.LogInformation("Found {0} Teams processes: {1}", teamsProcess.Count(), string.Join(", ", teamsProcess.Select(p => p.ProcessName)));

            return null;
        }
    }
}
