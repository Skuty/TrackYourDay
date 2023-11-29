namespace TrackYourDay.MAUI;

public partial class DialogPage : ContentPage
{
	public DialogPage(string startPath = null)
	{
		InitializeComponent();

        if (startPath != null)
        {
            this.blazorWebView.RootComponents[0].Parameters = new Dictionary<string, object>
            {
                { "StartPath", startPath },
            };
        }
    }
}