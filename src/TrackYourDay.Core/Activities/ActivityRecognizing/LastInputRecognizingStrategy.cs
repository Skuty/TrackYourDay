using System.Runtime.InteropServices;
using TrackYourDay.Core.Activities.SystemStates;

namespace TrackYourDay.Core.Activities.ActivityRecognizing
{
    public class LastInputRecognizingStrategy : ISystemStateRecognizingStrategy
    {
        public SystemState RecognizeActivity()
        {
            try
            {
                return new LastInputState(this.GetLastInputTime());
            } catch (Exception e)
            {
                return null;
            }
        }

        #region LastInput
        [DllImport("user32.dll")]
        static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        [StructLayout(LayoutKind.Sequential)]
        struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        public DateTime GetLastInputTime()
        {
            LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();
            lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);
            if (!GetLastInputInfo(ref lastInputInfo))
            {
                throw new Exception("Error getting last input info.");
            }
            return DateTime.Now.AddMilliseconds(-(Environment.TickCount - lastInputInfo.dwTime));
        }
        #endregion
    }
}
