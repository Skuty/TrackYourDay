using TrackYourDay.Core.Insights.Analytics;

namespace TrackYourDay.Core.ApplicationTrackers.UserTasks
{
    /// <summary>
    /// Represents a user-defined task that can be started and ended manually.
    /// Supports ongoing tasks (not yet ended).
    /// </summary>
    public sealed class UserTask : TrackableItem
    {
        private DateTime? _endDate;
        private string _description;
        
        public string Description
        {
            get => _description;
            private set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Description cannot be empty", nameof(value));
                _description = value;
            }
        }
        
        /// <summary>
        /// Override to support nullable end date for ongoing tasks.
        /// Returns DateTime.Now if task is not yet ended.
        /// </summary>
        public override DateTime EndDate => _endDate ?? DateTime.Now;
        
        /// <summary>
        /// Indicates whether the task has been completed.
        /// </summary>
        public bool IsCompleted => _endDate.HasValue;
        
        /// <summary>
        /// Factory method to start a new task.
        /// </summary>
        public static UserTask Start(DateTime startDate, string description)
        {
            return new UserTask(startDate, description);
        }
        
        private UserTask(DateTime startDate, string description) : base()
        {
            StartDate = startDate;
            Description = description;
            _endDate = null;
        }
        
        public override string GetDescription()
        {
            return Description;
        }
        
        /// <summary>
        /// Override to handle ongoing tasks (returns current duration if not ended).
        /// </summary>
        public override TimeSpan GetDuration()
        {
            var endTime = _endDate ?? DateTime.Now;
            return endTime - StartDate;
        }
        
        /// <summary>
        /// Updates the task description.
        /// </summary>
        public void UpdateDescription(string newDescription)
        {
            if (IsCompleted)
                throw new InvalidOperationException("Cannot update description of a completed task");
            
            if (string.IsNullOrWhiteSpace(newDescription))
                throw new ArgumentException("Description cannot be empty", nameof(newDescription));
            
            _description = newDescription;
        }
        
        /// <summary>
        /// Marks the task as completed.
        /// </summary>
        public void Complete(DateTime endDate)
        {
            if (IsCompleted)
                throw new InvalidOperationException("Task is already completed");
            
            if (endDate <= StartDate)
                throw new ArgumentException($"End date must be after start date ({StartDate})", nameof(endDate));
            
            _endDate = endDate;
        }
        
        /// <summary>
        /// Completes the task with the current time.
        /// </summary>
        public void CompleteNow()
        {
            Complete(DateTime.Now);
        }
    }
}
