using System;
using TrackYourDay.Core.ApplicationTrackers.UserTasks;
using Xunit;

namespace TrackYourDay.Tests.UserTasks
{
    public class UserTaskTests
    {
        [Fact]
        public void GivenValidParameters_WhenStartTaskIsCalled_ThenTaskIsCreatedSuccessfully()
        {
            // Arrange
            var startDate = DateTime.UtcNow;
            var description = "Test task";

            // Act
            var task = UserTask.StartTask(startDate, description);

            // Assert
            Assert.NotNull(task);
            Assert.NotEqual(Guid.Empty, task.Guid);
            Assert.Equal(startDate, task.StartDate);
            Assert.Equal(description, task.Description);
            Assert.Null(task.EndDate);
        }

        [Fact]
        public void GivenNullDescription_WhenSetDescriptionIsCalled_ThenArgumentNullExceptionIsThrown()
        {
            // Arrange
            var startDate = DateTime.UtcNow;
            var task = UserTask.StartTask(startDate, "Valid description");

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => task.SetDescription(null));
            Assert.Equal("description", exception.ParamName);
        }

        [Fact]
        public void GivenEmptyDescription_WhenSetDescriptionIsCalled_ThenArgumentNullExceptionIsThrown()
        {
            // Arrange
            var startDate = DateTime.UtcNow;
            var task = UserTask.StartTask(startDate, "Valid description");

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => task.SetDescription(string.Empty));
            Assert.Equal("description", exception.ParamName);
        }

        [Fact]
        public void GivenTaskNotEnded_WhenEndTaskIsCalledWithEarlierDate_ThenArgumentExceptionIsThrown()
        {
            // Arrange
            var startDate = DateTime.UtcNow;
            var task = UserTask.StartTask(startDate, "Valid description");
            var endDate = startDate.AddMinutes(-1);

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => task.EndTask(endDate));
        }

        [Fact]
        public void GivenTaskAlreadyEnded_WhenEndTaskIsCalled_ThenInvalidOperationExceptionIsThrown()
        {
            // Arrange
            var startDate = DateTime.UtcNow;
            var task = UserTask.StartTask(startDate, "Valid description");
            var endDate1 = startDate.AddMinutes(1);
            task.EndTask(endDate1);

            var endDate2 = startDate.AddMinutes(2);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => task.EndTask(endDate2));
            Assert.Equal("Can't end ended task", exception.Message);
        }

        [Fact]
        public void GivenValidParameters_WhenEndTaskIsCalled_ThenTaskEndDateIsSet()
        {
            // Arrange
            var startDate = DateTime.UtcNow;
            var task = UserTask.StartTask(startDate, "Valid description");
            var endDate = startDate.AddMinutes(1);

            // Act
            task.EndTask(endDate);

            // Assert
            Assert.Equal(endDate, task.EndDate);
        }
    }
}
