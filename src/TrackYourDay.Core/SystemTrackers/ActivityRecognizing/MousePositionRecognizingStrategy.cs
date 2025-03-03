using System.Runtime.InteropServices;
using TrackYourDay.Core.SystemTrackers.SystemStates;

namespace TrackYourDay.Core.SystemTrackers.ActivityRecognizing
{
    public class MousePositionRecognizingStrategy : ISystemStateRecognizingStrategy
    {
        public SystemState RecognizeActivity()
        {
            POINT cursorPos;
            if (GetCursorPos(out cursorPos))
            {
                return new MousePositionState(cursorPos.X, cursorPos.Y);
            }

            return new MousePositionState(-1, -1);
        }

        #region MousePosition
        [DllImport("user32.dll")]
        static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        struct POINT
        {
            public int X;
            public int Y;
        }
        #endregion
    }
}