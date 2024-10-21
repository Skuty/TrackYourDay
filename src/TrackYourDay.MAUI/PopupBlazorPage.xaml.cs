namespace TrackYourDay.MAUI
{
    public partial class PopupBlazorPage : ContentPage
    {
        public PopupBlazorPage(string path)
        {
            InitializeComponent();
            this.blazorWebViewSecond.StartPath = path;
        }

        // Send MediatR Command to close / minimize this window directly from BlazorView
        private void CloseThisWindow()
        {
            var currentWindow = Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault(w => w.Page is SimpleNotificationPage && w.Page.GetHashCode() == this.GetHashCode());
            Microsoft.Maui.Controls.Application.Current?.CloseWindow(currentWindow);
        }
    }
}