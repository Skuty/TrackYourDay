namespace UI.Core
{
    internal class MyActivitiesService : IObserver<SystemState>
    {
        private IDisposable unsubscriber;
        private IList<SystemState> notProcessedSystemStates;
        private IList<SystemState> processedSystemStates;
        private IList<MyActivity> activities;

        public MyActivitiesService()
        {
            this.notProcessedSystemStates = new List<SystemState>();
            this.processedSystemStates = new List<SystemState>();
            this.processedSystemStates.Add(SystemState.DUMMY);
            this.activities = new List<MyActivity>();
            this.activities.Add(new MyActivity(DateTime.Now, "Tracking started"));
        }

        public IList<MyActivity> GetActivities()
        {
            return activities;
        }

        public void AnalyzeNewSystemStates()
         {   
            //Change to fifo safe queue
            foreach (var systemState in notProcessedSystemStates)
            {
                if (this.processedSystemStates.Last().ActiveWindowName  != systemState.ActiveWindowName)
                {
                    var newActivity = new MyActivity(DateTime.Now, $"Working on {systemState.ActiveWindowName}");
                    this.activities.Last().End();
                    this.activities.Add( newActivity );

                } 
                
                processedSystemStates.Add(systemState);
            }

            notProcessedSystemStates.Clear();
        }

        private void OnSystemStateChange(SystemState systemState)
        {
            this.notProcessedSystemStates.Add(systemState);
            this.AnalyzeNewSystemStates();
        }


        #region Observer
        public virtual void Subscribe(IObservable<SystemState> provider)
        {
            if (provider != null)
                unsubscriber = provider.Subscribe(this);
        }

        public virtual void Unsubscribe()
        {
            unsubscriber.Dispose();
        }

        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public void OnNext(SystemState value)
        {
            this.OnSystemStateChange(value);
        }
        #endregion
    }
}