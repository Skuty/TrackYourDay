using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TrackYourDay.Tests.Old.Old.Activities;

namespace TrackYourDay.Tests.Old.Old.Activities.RecognizingStrategies
{
    public class WindowNameActivityRecognizingStrategy : IActivityRecognizingStrategy
    {
        public Activity RecognizeActivity()
        {
            var currentActiveWindowName = GetCaptionOfActiveWindow();

            if (currentActiveWindowName == "ekran blokady")
            {
                return new SystemLockedActivity();
            }

            return new FocusOnApplicationActivity(currentActiveWindowName);
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
