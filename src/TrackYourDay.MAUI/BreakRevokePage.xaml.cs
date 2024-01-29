using Microsoft.UI.Xaml;
using TrackYourDay.Core.Breaks;
using TrackYourDay.MAUI.MauiPages;

namespace TrackYourDay.MAUI;

public partial class BreakRevokePage : ContentPage
{
    private readonly Guid breakGuid;
    private readonly BreakTracker breakTracker;
    private readonly DispatcherTimer timer = new DispatcherTimer();
    private readonly TimeSpan showPeriod;
    private readonly BreakRevokeViewModel breakRevokeViewModel;
    private double counterStep;

    public BreakRevokePage(Guid breakGuid, BreakTracker breakTracker)
	{
		InitializeComponent();

        this.breakGuid = breakGuid;
        this.breakTracker = breakTracker;
        this.showPeriod = TimeSpan.FromSeconds(120);
        this.counterStep = 1 / (this.showPeriod.TotalSeconds * 4);

        // Todo publish Break with breakGuid instead of just guid or other kind of readmodel
        var endedBreak = this.breakTracker.GetEndedBreaks().First(b => b.BreakGuid == breakGuid);

        // TODO: Move UI text to xaml as use only values from view model
        this.breakRevokeViewModel = new BreakRevokeViewModel()
        {
            BreakDuration = $"Break duration: {(int)endedBreak.BreakDuration.TotalMinutes} minutes",
            BreakBorders = $"From: {endedBreak.BreakStartedAt.ToShortTimeString()} to {endedBreak.BreakEndedAt.ToShortTimeString()}",
        };

        this.breakPeriodLabel.BindingContext = this.breakRevokeViewModel;
        this.breakBordersLabel.BindingContext = this.breakRevokeViewModel;

        this.breakPeriodLabel.SetBinding(Label.TextProperty, "BreakDuration");
        this.breakBordersLabel.SetBinding(Label.TextProperty, "BreakBorders");

        this.timer.Interval = TimeSpan.FromMilliseconds(250);
        this.timer.Tick += Timer_Tick;
        this.timer.Start();
    }

    private void Timer_Tick(object sender, object e)
    {
        this.progressBar.Progress += this.counterStep;
        if (this.progressBar.Progress >= 1)
        {
            this.timer.Stop();
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