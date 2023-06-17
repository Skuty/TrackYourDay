using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrackYourDay.Core
{
    public class Features
    {
        public Features(bool isBreakRecordingEnabled)
        {
            this.IsBreakRecordingEnabled = isBreakRecordingEnabled;
        }

        public bool IsBreakRecordingEnabled { get; }
    }
}
