using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.Core
{
    internal class SystemState
    {
        public static SystemState DUMMY => new SystemState(DateTime.Now, "Test Window");

        public SystemState(DateTime stateOnDate, string activeWindowName)
        {
            StateOnDate = stateOnDate;
            ActiveWindowName = activeWindowName ?? string.Empty;
        }

        public SystemState(string activeWindowName) : this(DateTime.Now, activeWindowName)
        {
        }

        public DateTime StateOnDate { get; }

        public string ActiveWindowName { get; }

        public bool IsSystemLocked => ActiveWindowName.Contains("ekran blokady") ? true : false;
    }
}
