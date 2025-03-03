namespace TrackYourDay.Core.ApplicationTrackers.UserTasks
{
    public class UserTaskService
    {
        private readonly List<UserTask> _tasks;

        public UserTaskService()
        {
            _tasks = new List<UserTask>();
        }

        // Get all tasks
        public IEnumerable<UserTask> GetAllTasks()
        {
            return _tasks.AsReadOnly();
        }

        // Get task by Guid
        public UserTask GetTaskById(Guid taskId)
        {
            var task = _tasks.FirstOrDefault(t => t.Guid == taskId);
            if (task == null)
            {
                throw new InvalidOperationException("Task not found");
            }

            return task;
        }

        // Start a new task
        public UserTask StartTask(DateTime startDate, string description)
        {
            // Ensure no active task exists
            if (_tasks.Any(t => t.EndDate == null))
            {
                throw new InvalidOperationException("There is already an active task");
            }

            // Create new task
            var task = UserTask.StartTask(startDate, description);
            _tasks.Add(task);

            return task;
        }

        // End a task
        public void EndTask(Guid taskId, DateTime endDate)
        {
            var task = GetTaskById(taskId);

            task.EndTask(endDate);
        }

        // Get the currently active task, if any
        public UserTask GetActiveTask()
        {
            return _tasks.FirstOrDefault(t => t.EndDate == null);
        }
    }
}
