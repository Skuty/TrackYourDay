namespace TrackYourDay.MAUI
{
    public partial class PopupBlazorPage : ContentPage
    {
        public PopupBlazorPage()
        {
            InitializeComponent();
            this.blazorWebViewSecond.StartPath = "/popupUserTask";
        }
    }
}