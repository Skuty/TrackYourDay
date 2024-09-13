namespace TrackYourDay.Core.UserTasks
{
    internal class UserTask
    {
        public Guid Guid { get; init; }

        public DateTime StartDate { get; init; }

        public DateTime? EndDate { get; private set; }

        public string Description { get; private set; }

        public static UserTask StartTask(DateTime startDate, string description)
        {
            var task = new UserTask()
            {
                Guid = Guid.NewGuid(),
                StartDate = startDate,
            };

            task.SetDescription(description);

            return task;
        }

        public void SetDescription(string description)
        {
            if (string.IsNullOrEmpty(description))
            {
                throw new ArgumentNullException("description");
            }

            this.Description = description;
        }

        public void EndTask(DateTime endDate)
        {
            if (this.EndDate is not null)
            {
                throw new InvalidOperationException("Can't end ended task");
            }

            if (endDate <= this.StartDate)
            {
                throw new ArgumentException($"Have to later than {this.StartDate}", nameof(endDate));
            }

            this.EndDate = endDate;
        }
    }
}
