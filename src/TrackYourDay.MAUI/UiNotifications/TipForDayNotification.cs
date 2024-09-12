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
        private readonly List<string> advices;

        public TipForDayNotification() : base()
        {
            this.IsEnabled = true;
            this.advices.Add("Porada na dziś: Zacznij kończyć, przestań zaczynać :)");
            this.advices.Add("Porada na dziś: Zaakceptuj niedoskonałe, nie wszystko musi być idealne :)");
            this.advices.Add("Porada na dziś: Szukaj okazji, a nie przeszkód :)");
            this.advices.Add("Porada na dziś: Uśmiechnij się :]");
            this.advices.Add("Porada na dziś: Pamiętaj o przerwach! Twój Organizm ich potrzebuje!");
            this.advices.Add("Czy wiesz że: Drzewa emitują związki chemiczne zwane fitoncydami, które mają właściwości antybakteryjne. Badania wykazały, że przebywanie w lasach i wdychanie tych związków może wspomagać układ odpornościowy człowieka.");
            this.advices.Add("Czy wiesz że: Mikrobiom jelitowy, czyli zbiór mikroorganizmów żyjących w naszym układzie pokarmowym, wpływa na nasz nastrój, zdrowie psychiczne, trawienie, a nawet układ odpornościowy. Właściwa dieta wspierająca dobre bakterie ma zatem bezpośredni wpływ na nasze samopoczucie.");
            this.advices.Add("Około 75% roślin uprawnych na świecie jest zależnych od zapylaczy, takich jak pszczoły. Dzięki nim mamy dostęp do wielu owoców i warzyw, bez których nasza dieta byłaby znacznie uboższa.");
        }

        public override bool ShouldBeExecuted()
        {
            return base.ShouldBeExecuted();
        }

        public override void Execute()
        {
            base.Execute();

            var advice = this.advices[Random.Shared.Next(this.advices.Count)];

            MauiPageFactory.OpenSimpleNotificationPageInNewWindow(new SimpleNotificationViewModel(
                    "Miłego dnia pracy!",
                    advice));
        }
    }
}