using TrackYourDay.Core.Insights.Analytics;

namespace TrackYourDay.Core.ApplicationTrackers.UserTasks
{
    public class UserTask : ITrackableItem
    {
        public Guid Guid { get; init; }

        public DateTime StartDate { get; init; }

        public DateTime? EndDate { get; private set; }

        public string Description { get; private set; }

        DateTime ITrackableItem.EndDate => EndDate ?? DateTime.Now;

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

            Description = description;
        }

        public void EndTask(DateTime endDate)
        {
            if (EndDate is not null)
            {
                throw new InvalidOperationException("Can't end ended task");
            }

            if (endDate <= StartDate)
            {
                throw new ArgumentException($"Have to later than {StartDate}", nameof(endDate));
            }

            EndDate = endDate;
        }

        public TimeSpan GetDuration()
        {
            return EndDate is not null 
                ? TimeSpan.FromTicks(EndDate.Value.Ticks - StartDate.Ticks) 
                : TimeSpan.FromTicks(DateTime.Now.Ticks - StartDate.Ticks);
        }

        public string GetDescription()
        {
            return Description;
        }
    }
}
