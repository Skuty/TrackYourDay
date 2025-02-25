using MediatR;
using Microsoft.UI.Xaml;
using TrackYourDay.MAUI.MauiPages;

namespace TrackYourDay.MAUI;

public partial class SimpleNotificationPage : ContentPage
{
    private readonly DispatcherTimer timer = new DispatcherTimer();
    private readonly TimeSpan showPeriod;
    private readonly IMediator mediator;
    private double counterStep;

    public SimpleNotificationPage(SimpleNotificationViewModel simpleNotificationViewModel, IMediator mediator)
    {
        InitializeComponent();

        this.mediator = mediator;

        this.showPeriod = TimeSpan.FromSeconds(120);
        this.counterStep = 1 / (this.showPeriod.TotalSeconds * 4);

        this.titleLabel.BindingContext = simpleNotificationViewModel;
        this.contentLabel.BindingContext = simpleNotificationViewModel;

        this.titleLabel.SetBinding(Label.TextProperty, "Title");
        this.contentLabel.SetBinding(Label.TextProperty, "Content");

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
            this.mediator.Send(new CloseWindowCommandHandler(this.Id));
        }
    }

	public void OnOkButtonClicked(object sender, EventArgs args)
	{
        this.mediator.Send(new CloseWindowCommandHandler(this.Id));
    }
}