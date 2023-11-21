namespace TrackYourDay.WPFUI
{
    internal class SharedInstance : ISharedInstance
    {
        private int counter = 0;

        public void Increment()
        {
            this.counter++;
        }

        public int GetCounter()
        {
            return this.counter;
        }
    }
}
