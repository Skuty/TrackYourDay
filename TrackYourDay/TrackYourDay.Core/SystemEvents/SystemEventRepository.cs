using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrackYourDay.Core.Events
{
    public class SystemEventRepository
    {
        private List<SystemEvent> Events;

        public SystemEventRepository()
        {
            Events = new List<SystemEvent>();
        }

        public void SaveEvent(SystemEvent @event)
        {
            Events.Add(@event);
        }
    }
}
