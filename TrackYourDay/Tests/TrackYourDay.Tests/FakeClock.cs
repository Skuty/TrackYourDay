using TrackYourDay.Core;

namespace TrackYourDay.Tests
{
    internal class FakeClock : IClock
    {
        private DateTime date = DateTime.Today;
        public DateTime Now => this.date;

        public void SetDate(DateTime date)
        {
            this.date = date;
        }
    }
}
