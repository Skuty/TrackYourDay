namespace UI.Core
{
    internal class MyActivity
    {
        public MyActivity(DateTime started, DateTime ended, string description)
        {
            this.Started = started;
            this.Ended = ended;
            Description = description;
        }

        public MyActivity(DateTime started, string description)
        {
            this.Started = started;
            Description = description;
        }


        public DateTime Started { get; }

        public DateTime Ended { get; private set; }

        public string Description { get; }

        public void End()
        {
            this.Ended = DateTime.Now;
        }
    }
}