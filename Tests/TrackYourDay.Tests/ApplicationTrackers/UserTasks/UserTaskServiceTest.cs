using TrackYourDay.Core.ApplicationTrackers.UserTasks;

namespace TrackYourDay.Tests.ApplicationTrackers.UserTasks
{
    public class UserTaskServiceTests
    {
        [Fact]
        public void GivenNoActiveTask_WhenStartTaskIsCalled_ThenTaskIsStartedSuccessfully()
        {
            // Arrange
            var taskService = new UserTaskService();
            var startDate = DateTime.UtcNow;
            var description = "Test task";

            // Act
            var task = taskService.StartTask(startDate, description);

            // Assert
            Assert.NotNull(task);
            Assert.Equal(startDate, task.StartDate);
            Assert.Equal(description, task.Description);
            Assert.False(task.IsCompleted);
        }

        [Fact]
        public void GivenActiveTask_WhenStartTaskIsCalled_ThenInvalidOperationExceptionIsThrown()
        {
            // Arrange
            var taskService = new UserTaskService();
            var startDate = DateTime.UtcNow;
            taskService.StartTask(startDate, "First task");

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                taskService.StartTask(startDate.AddMinutes(10), "Second task"));
            Assert.Equal("There is already an active task", exception.Message);
        }

        [Fact]
        public void GivenTaskStarted_WhenEndTaskIsCalled_ThenTaskIsEndedSuccessfully()
        {
            // Arrange
            var taskService = new UserTaskService();
            var startDate = DateTime.UtcNow;
            var task = taskService.StartTask(startDate, "Test task");
            var endDate = startDate.AddMinutes(10);

            // Act
            taskService.EndTask(task.Guid, endDate);

            // Assert
            var endedTask = taskService.GetTaskById(task.Guid);
            Assert.Equal(endDate, endedTask.EndDate);
            Assert.True(endedTask.IsCompleted);
        }

        [Fact]
        public void GivenNoActiveTask_WhenGetActiveTaskIsCalled_ThenNullIsReturned()
        {
            // Arrange
            var taskService = new UserTaskService();

            // Act
            var activeTask = taskService.GetActiveTask();

            // Assert
            Assert.Null(activeTask);
        }

        [Fact]
        public void GivenActiveTask_WhenGetActiveTaskIsCalled_ThenActiveTaskIsReturned()
        {
            // Arrange
            var taskService = new UserTaskService();
            var startDate = DateTime.UtcNow;
            var task = taskService.StartTask(startDate, "Active task");

            // Act
            var activeTask = taskService.GetActiveTask();

            // Assert
            Assert.NotNull(activeTask);
            Assert.Equal(task.Guid, activeTask.Guid);
        }

        [Fact]
        public void GivenTaskAlreadyEnded_WhenEndTaskIsCalled_ThenInvalidOperationExceptionIsThrown()
        {
            // Arrange
            var taskService = new UserTaskService();
            var startDate = DateTime.UtcNow;
            var task = taskService.StartTask(startDate, "Test task");
            var endDate = startDate.AddMinutes(10);
            taskService.EndTask(task.Guid, endDate);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                taskService.EndTask(task.Guid, endDate.AddMinutes(5)));
            Assert.Equal("Task is already completed", exception.Message);
        }
    }
}
