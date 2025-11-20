using Microsoft.UI.Xaml;
using TrackYourDay.Core.ApplicationTrackers.MsTeams;
using TrackYourDay.MAUI.MauiPages;

namespace TrackYourDay.MAUI;

public partial class MeetingDescriptionPage : ContentPage
{
    private readonly DispatcherTimer timer = new DispatcherTimer();
    private readonly TimeSpan showPeriod;
    private double counterStep;

    private readonly Guid meetingGuid;
    private readonly MsTeamsMeetingTracker meetingTracker;
    private readonly MeetingDescriptionViewModel meetingDescriptionViewModel;

    public MeetingDescriptionPage(Guid meetingGuid, MsTeamsMeetingTracker meetingTracker)
	{
		InitializeComponent();

        this.meetingGuid = meetingGuid;
        this.meetingTracker = meetingTracker;
        this.showPeriod = TimeSpan.FromSeconds(120);
        this.counterStep = 1 / (this.showPeriod.TotalSeconds * 4);

        var endedMeeting = this.meetingTracker.GetEndedMeetings().FirstOrDefault(m => m.Guid == meetingGuid);
        
        if (endedMeeting == null)
        {
            throw new InvalidOperationException("Meeting not found in the tracker");
        }

        this.meetingDescriptionViewModel = new MeetingDescriptionViewModel()
        {
            MeetingTitle = $"Meeting: {endedMeeting.Title}",
            MeetingDuration = $"Duration: {(int)endedMeeting.GetDuration().TotalMinutes} minutes",
            MeetingBorders = $"From: {endedMeeting.StartDate.ToShortTimeString()} to {endedMeeting.EndDate.ToShortTimeString()}",
        };

        this.meetingTitleLabel.BindingContext = this.meetingDescriptionViewModel;
        this.meetingDurationLabel.BindingContext = this.meetingDescriptionViewModel;
        this.meetingBordersLabel.BindingContext = this.meetingDescriptionViewModel;

        this.meetingTitleLabel.SetBinding(Label.TextProperty, "MeetingTitle");
        this.meetingDurationLabel.SetBinding(Label.TextProperty, "MeetingDuration");
        this.meetingBordersLabel.SetBinding(Label.TextProperty, "MeetingBorders");

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

    public async void OnSaveDescriptionButtonClicked(object sender, EventArgs args)
	{ 
        try
        {
            var description = this.descriptionEntry.Text;
            if (!string.IsNullOrWhiteSpace(description))
            {
                var endedMeeting = this.meetingTracker.GetEndedMeetings().FirstOrDefault(m => m.Guid == this.meetingGuid);
                if (endedMeeting != null)
                {
                    endedMeeting.SetDescription(description);
                }
            }
        } 
        catch (Exception)
        {
            await this.DisplayAlert("Failed to save meeting description", "An error occurred while saving the description. Please try again.", "OK");
            return;
        }

        this.CloseThisWindow();
    }

	public void OnSkipButtonClicked(object sender, EventArgs args)
	{
        this.CloseThisWindow();
    }

    private void CloseThisWindow()
    {
        var currentWindow = Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault(w => w.Page is MeetingDescriptionPage);
        Microsoft.Maui.Controls.Application.Current?.CloseWindow(currentWindow);
    }
}
