namespace TrackYourDay.Core
{
    internal class Clock : IClock
    {
        public DateTime Now => DateTime.Now;
    }
}
