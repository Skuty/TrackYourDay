using TrackYourDay.Web;

namespace TrackYourDay.MAUI
{
    public partial class PopupBlazorPage : ContentPage
    {
        public PopupBlazorPage(string path)
        {
            var temp = new ReferencyClass();
            InitializeComponent();
            this.blazorWebViewSecond.StartPath = path;

            // TODO: this could be turned into named root component in xaml file
            this.blazorWebViewSecond.RootComponents.First().Parameters 
                = new Dictionary<string, object>()
          {
                { "ParentMauiWindowId", this.Id }
           };
        }

        // Send MediatR Command to close / minimize this window directly from BlazorView
        private void CloseThisWindow()
        {
            var currentWindow = Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault(w => w.Page is PopupBlazorPage && w.Page.GetHashCode() == this.GetHashCode());
            Microsoft.Maui.Controls.Application.Current?.CloseWindow(currentWindow);
        }
    }
}