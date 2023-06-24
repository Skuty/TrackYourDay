using System.Runtime.InteropServices;
using System.Text;

namespace UI.Core
{
    public class SystemStateTracker : IObservable<SystemState>
    {
        private List<IObserver<SystemState>> observers;

        public SystemStateTracker()
        {

            this.observers = new List<IObserver<SystemState>>();
        }

        public void PublishSystemState()
        {
            var systemState = this.GetSystemState();

            foreach (var observer in observers)
            {
                observer.OnNext(systemState);
            }
        }

        private SystemState GetSystemState()
        {

            var currentActiveWindowName = GetCaptionOfActiveWindow();
            return new SystemState(currentActiveWindowName);
        }

        #region WindowName
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int GetWindowTextLength(IntPtr hWnd);

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
        #endregion

        #region Observer
        IDisposable IObservable<SystemState>.Subscribe(IObserver<SystemState> observer)
        {
            if (!observers.Contains(observer))
                observers.Add(observer);
            return new Unsubscriber(observers, observer);
        }

        private class Unsubscriber : IDisposable
        {
            private List<IObserver<SystemState>> _observers;
            private IObserver<SystemState> _observer;

            public Unsubscriber(List<IObserver<SystemState>> observers, IObserver<SystemState> observer)
            {
                this._observers = observers;
                this._observer = observer;
            }

            public void Dispose()
            {
                if (_observer != null && _observers.Contains(_observer))
                    _observers.Remove(_observer);
            }
        }
        #endregion
    }
}