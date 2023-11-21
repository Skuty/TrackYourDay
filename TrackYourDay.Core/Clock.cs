namespace TrackYourDay.Core
{
    public class Clock : IClock
    {
        public DateTime Now => DateTime.Now;
    }
}
