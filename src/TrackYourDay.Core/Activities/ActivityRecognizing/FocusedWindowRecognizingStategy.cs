using System.Runtime.InteropServices;
using System.Text;
using TrackYourDay.Core.Activities.SystemStates;

namespace TrackYourDay.Core.Activities.ActivityRecognizing
{
    public class FocusedWindowRecognizingStategy : ISystemStateRecognizingStrategy
    {
        public SystemState RecognizeActivity()
        {
            var currentActiveWindowName = GetCaptionOfActiveWindow();

            if (currentActiveWindowName.Contains("ekran blokady"))
            {
                return SystemStateFactory.SystemLockedState();
            }

            return SystemStateFactory.FocusOnApplicationState(currentActiveWindowName);
        }

        #region WindowName
        private string GetCaptionOfActiveWindow()
        {
            var strTitle = string.Empty;
            var handle = GetForegroundWindow();
            // Obtain the length of the text   
            var intLength = GetWindowTextLength(handle) + 1;
            var stringBuilder = new StringBuilder(intLength);

            if (GetWindowText(handle, stringBuilder, intLength) > 0)
            {
                strTitle = stringBuilder.ToString();
            }

            return strTitle;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern nint GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int GetWindowText(nint hWnd, StringBuilder text, int count);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int GetWindowTextLength(nint hWnd);
        #endregion
    }
}
