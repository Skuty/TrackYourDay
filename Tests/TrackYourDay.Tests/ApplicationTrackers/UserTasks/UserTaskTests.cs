using System;
using TrackYourDay.Core.ApplicationTrackers.UserTasks;
using Xunit;

namespace TrackYourDay.Tests.ApplicationTrackers.UserTasks
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
            var task = UserTask.Start(startDate, description);

            // Assert
            Assert.NotNull(task);
            Assert.NotEqual(Guid.Empty, task.Guid);
            Assert.Equal(startDate, task.StartDate);
            Assert.Equal(description, task.Description);
            Assert.False(task.IsCompleted);
        }

        [Fact]
        public void GivenNullDescription_WhenUpdateDescriptionIsCalled_ThenArgumentExceptionIsThrown()
        {
            // Arrange
            var startDate = DateTime.UtcNow;
            var task = UserTask.Start(startDate, "Valid description");

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => task.UpdateDescription(null));
            Assert.Equal("newDescription", exception.ParamName);
        }

        [Fact]
        public void GivenEmptyDescription_WhenUpdateDescriptionIsCalled_ThenArgumentExceptionIsThrown()
        {
            // Arrange
            var startDate = DateTime.UtcNow;
            var task = UserTask.Start(startDate, "Valid description");

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => task.UpdateDescription(string.Empty));
            Assert.Equal("newDescription", exception.ParamName);
        }

        [Fact]
        public void GivenTaskNotEnded_WhenCompleteIsCalledWithEarlierDate_ThenArgumentExceptionIsThrown()
        {
            // Arrange
            var startDate = DateTime.UtcNow;
            var task = UserTask.Start(startDate, "Valid description");
            var endDate = startDate.AddMinutes(-1);

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => task.Complete(endDate));
        }

        [Fact]
        public void GivenTaskAlreadyEnded_WhenCompleteIsCalled_ThenInvalidOperationExceptionIsThrown()
        {
            // Arrange
            var startDate = DateTime.UtcNow;
            var task = UserTask.Start(startDate, "Valid description");
            var endDate1 = startDate.AddMinutes(1);
            task.Complete(endDate1);

            var endDate2 = startDate.AddMinutes(2);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => task.Complete(endDate2));
            Assert.Equal("Task is already completed", exception.Message);
        }

        [Fact]
        public void GivenValidParameters_WhenCompleteIsCalled_ThenTaskEndDateIsSet()
        {
            // Arrange
            var startDate = DateTime.UtcNow;
            var task = UserTask.Start(startDate, "Valid description");
            var endDate = startDate.AddMinutes(1);

            // Act
            task.Complete(endDate);

            // Assert
            Assert.Equal(endDate, task.EndDate);
            Assert.True(task.IsCompleted);
        }
    }
}
