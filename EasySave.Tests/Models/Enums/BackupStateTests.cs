using Models.Enums;
using Xunit;

namespace EasySave.Tests.Models.Enums
{
    public class BackupStateTests
    {
        [Fact]
        public void BackupState_HasActiveValue()
        {
            // Arrange & Act
            var state = BackupState.ACTIVE;

            // Assert
            Assert.Equal(BackupState.ACTIVE, state);
        }

        [Fact]
        public void BackupState_HasPausedValue()
        {
            // Arrange & Act
            var state = BackupState.PAUSED;

            // Assert
            Assert.Equal(BackupState.PAUSED, state);
        }

        [Fact]
        public void BackupState_HasCompletedValue()
        {
            // Arrange & Act
            var state = BackupState.COMPLETED;

            // Assert
            Assert.Equal(BackupState.COMPLETED, state);
        }

        [Fact]
        public void BackupState_HasErrorValue()
        {
            // Arrange & Act
            var state = BackupState.ERROR;

            // Assert
            Assert.Equal(BackupState.ERROR, state);
        }

        [Fact]
        public void BackupState_HasPendingValue()
        {
            // Arrange & Act
            var state = BackupState.PENDING;

            // Assert
            Assert.Equal(BackupState.PENDING, state);
        }

        [Theory]
        [InlineData(BackupState.ACTIVE)]
        [InlineData(BackupState.PAUSED)]
        [InlineData(BackupState.COMPLETED)]
        [InlineData(BackupState.ERROR)]
        [InlineData(BackupState.PENDING)]
        public void BackupState_AllValuesAreValid(BackupState state)
        {
            // Assert
            Assert.True(System.Enum.IsDefined(typeof(BackupState), state));
        }
    }
}