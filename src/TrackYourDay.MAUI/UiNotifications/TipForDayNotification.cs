using TrackYourDay.Core.Notifications;
using TrackYourDay.MAUI.MauiPages;

/// <summary>
/// Draft of Notification
/// </summary>
/// <TODO>
/// Handle persisting different configuration for different notifications (conditions, etc)
/// </TODO>
namespace TrackYourDay.MAUI.UiNotifications
{
    public class TipForDayNotification : ExecutableNotification
    {
        public TipForDayNotification() : base()
        {
            this.IsEnabled = true;
        }

        public override bool ShouldBeExecuted()
        {
            return base.ShouldBeExecuted();
        }

        public override void Execute()
        {
            base.Execute();

            MauiPageFactory.OpenSimpleNotificationPageInNewWindow(new SimpleNotificationViewModel(
                    "Miłego dnia pracy!",
                    $"Porada na dziś: Może zaproponuj jakąś? :)"));
        }
    }
}