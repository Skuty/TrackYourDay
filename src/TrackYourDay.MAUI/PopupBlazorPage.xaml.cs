namespace TrackYourDay.MAUI
{
    public partial class PopupBlazorPage : ContentPage
    {
        public PopupBlazorPage(string path)
        {
            InitializeComponent();
            this.blazorWebViewSecond.StartPath = path;
        }
    }
}