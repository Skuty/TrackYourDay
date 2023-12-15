using Microsoft.UI.Xaml;
using TrackYourDay.Core.Breaks;

namespace TrackYourDay.MAUI;

public partial class BreakRevokePage : ContentPage
{
    private readonly Guid breakGuid;
    private readonly BreakTracker breakTracker;
    private readonly DispatcherTimer timer = new DispatcherTimer();


    public BreakRevokePage(Guid breakGuid, BreakTracker breakTracker)
	{
		InitializeComponent();

        this.breakGuid = breakGuid;
        this.breakTracker = breakTracker;


        timer.Interval = TimeSpan.FromSeconds(1);
        timer.Tick += Timer_Tick;
        timer.Start();
    }

    private void Timer_Tick(object sender, object e)
    {
        this.progressBar.value ++
        if (this.progressBar.Value >= 60)
        {
            timer.Stop();
        }
    }

    public void OnRevokeBreakButtonClicked(object sender, EventArgs args)
	{ 
        this.breakTracker.RevokeBreak(this.breakGuid, DateTime.Now);
        this.CloseThisWindow();
    }

	public void OnCancelRevokeBreakButtonClicked(object sender, EventArgs args)
	{
        this.CloseThisWindow();
    }

    private void CloseThisWindow()
    {
        var currentWindow = Application.Current?.Windows.FirstOrDefault(w => w.Page is BreakRevokePage);
        Application.Current?.CloseWindow(currentWindow);
    }
}