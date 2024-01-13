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

    public string forAroundMinutes { get; set; } = "forAroundAmount";
    public string fromHour { get; set; } = "fromHour";
    public string toHour { get; set; } = "toHour";

    // Todo publish Break with breakGuid instead of just guid or other kind of readmodel
    public BreakRevokePage(Guid breakGuid, BreakTracker breakTracker)
	{
		InitializeComponent();

        //breakPeriodLabel.SetBinding(Label.TextProperty, new Binding("breakPeriodBinding", source: this.forAroundMinutes));
        //breakStartLabel.SetBinding(Label.TextProperty, new Binding("breakStartBinding", source: this.fromHour));
        //breakEndLabel.SetBinding(Label.TextProperty, new Binding("breakEndLabel", source: this.toHour));


        this.breakGuid = breakGuid;
        this.breakTracker = breakTracker;
        this.showPeriod = TimeSpan.FromSeconds(120);
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