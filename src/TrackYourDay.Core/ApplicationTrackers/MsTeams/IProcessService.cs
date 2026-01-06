using System.Diagnostics;
using System.Linq;

namespace TrackYourDay.Core.ApplicationTrackers.MsTeams
{
    public interface IProcessService
    {
        IEnumerable<IProcess> GetProcesses();
    }

    public class WindowsProcessService : IProcessService
    {
        public IEnumerable<IProcess> GetProcesses()
        {
            return Process.GetProcesses()
                          .Select(p => new ProcessSnapshot
                          {
                              ProcessName = p.ProcessName,
                              MainWindowTitle = p.MainWindowTitle
                          });
        }
    }

    public interface IProcess
    {
        string ProcessName { get; }
        string MainWindowTitle { get; }
    }

    public class ProcessSnapshot : IProcess
    {
        public string ProcessName { get; set; }
        public string MainWindowTitle { get; set; }
    }
}