using Microsoft.UI.Xaml;
using TrackYourDay.Core.Breaks;

namespace TrackYourDay.MAUI;

public partial class BreakRevokePage : ContentPage
{
    private readonly Guid breakGuid;
    private readonly BreakTracker breakTracker;
    private readonly DispatcherTimer timer = new DispatcherTimer();
    private readonly TimeSpan showPeriod;
    private double counterStep; 

    public BreakRevokePage(Guid breakGuid, BreakTracker breakTracker)
	{
		InitializeComponent();

        this.breakGuid = breakGuid;
        this.breakTracker = breakTracker;
        this.showPeriod = TimeSpan.FromSeconds(15);
        this.counterStep = 1 / (this.showPeriod.TotalSeconds * 4);

        timer.Interval = TimeSpan.FromMilliseconds(250);
        timer.Tick += Timer_Tick;
        timer.Start();
    }

    private void Timer_Tick(object sender, object e)
    {
        this.progressBar.Progress += this.counterStep;
        if (this.progressBar.Progress >= 1)
        {
            timer.Stop();
            this.CloseThisWindow();
        }
    }

    public void OnRevokeBreakButtonClicked(object sender, EventArgs args)
	{ 
        try
        {
            this.breakTracker.RevokeBreak(this.breakGuid, DateTime.Now);
        } catch (Exception ex)
        {
            this.DisplayAlert("Something went wrong", $"{ex}", "OK");
            return;
        }

        this.CloseThisWindow();
    }

	public void OnCancelRevokeBreakButtonClicked(object sender, EventArgs args)
	{
        this.CloseThisWindow();
    }

    private void CloseThisWindow()
    {
        var currentWindow = Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault(w => w.Page is BreakRevokePage);
        Microsoft.Maui.Controls.Application.Current?.CloseWindow(currentWindow);
    }
}