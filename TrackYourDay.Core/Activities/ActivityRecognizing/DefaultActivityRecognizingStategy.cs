using System.Runtime.InteropServices;
using System.Text;
using TrackYourDay.Core.Activities;

namespace TrackYourDay.Core.Old.Activities.RecognizingStrategies
{
    public class DefaultActivityRecognizingStategy : IStartedActivityRecognizingStrategy
    {
        ActivityType IStartedActivityRecognizingStrategy.RecognizeActivity()
        {
            var currentActiveWindowName = GetCaptionOfActiveWindow();

            if (currentActiveWindowName == "ekran blokady")
            {
                return ActivityTypeFactory.SystemLockedActivityType();
            }

            return ActivityTypeFactory.FocusOnApplicationActivityType(currentActiveWindowName);
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
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int GetWindowTextLength(IntPtr hWnd);
        #endregion
    }
}
