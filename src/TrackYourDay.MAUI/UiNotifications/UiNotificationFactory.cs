using MediatR;
using TrackYourDay.Core.Notifications;
using TrackYourDay.Core.Workdays;
using TrackYourDay.MAUI.MauiPages;

/// <summary>
/// Draft of Notification
/// </summary>
/// <TODO>
/// Handle persisting different configuration for different notifications (conditions, etc)
/// </TODO>
namespace TrackYourDay.MAUI.UiNotifications
{
    public class UiNotificationFactory : INotificationFactory
    {
        private readonly IMediator mediator;
        private readonly WorkdayReadModelRepository workdayReadModelRepository;
        private readonly MauiPageFactory mauiPageFactory;

        public UiNotificationFactory(IMediator mediator, WorkdayReadModelRepository workdayReadModelRepository)
        {
            this.mediator = mediator;
            this.workdayReadModelRepository = workdayReadModelRepository;
            this.mauiPageFactory = new MauiPageFactory(mediator);
        }

        public ExecutableNotification GetNotificationByName(string name)
        {
            if (name == "EndOfWorkdayNear")
            {
                return new EndOfWorkDayNearNotification(TimeSpan.FromMinutes(45), workdayReadModelRepository, this.mauiPageFactory);
            }

            if (name == "EndOfWorkday")
            {
                return new EndOfWorkDayNotification(workdayReadModelRepository, this.mauiPageFactory);
            }

            throw new NotImplementedException();
        }

        public ExecutableNotification GetDefaultNotification()
        {
            return new TipForDayNotification(this.mauiPageFactory);
        }
    }
}