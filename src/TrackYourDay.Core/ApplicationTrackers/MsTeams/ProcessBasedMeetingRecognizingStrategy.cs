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

            foreach (var process in teamsProcess)
            {
                this.logger.LogInformation("Found Teams process! Name: {0}, Title: {1}", process.ProcessName, process.MainWindowTitle);
            }

            foreach (var process in teamsProcess)
            {
                var processTitle = process.MainWindowTitle;
                if (processTitle.Contains("spotkanie", StringComparison.InvariantCultureIgnoreCase)
                    || processTitle.Contains("Widok kompaktowy spotkania", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!processTitle.Contains("Czat", StringComparison.InvariantCultureIgnoreCase))
                    {
                        // This should only return meeting, guid should be applied above that based on other comparisions
                        return new StartedMeeting(Guid.NewGuid(), DateTime.Now, process.MainWindowTitle);
                    }
                }
            }

            return null;
        }
    }
}
