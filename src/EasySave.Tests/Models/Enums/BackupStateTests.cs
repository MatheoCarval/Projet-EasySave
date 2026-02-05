using Models.Enums;
using Xunit;

namespace EasySave.Tests.Models.Enums
{
    /// <summary>
    /// Unit tests for the BackupState enumeration, verifying all enum values are defined and accessible.
    /// </summary>
    public class BackupStateTests
    {
        /// <summary>
        /// Verifies that the BackupState enumeration contains the ACTIVE value.
        /// </summary>
        [Fact]
        public void BackupState_HasActiveValue()
        {
            var state = BackupState.ACTIVE;

            Assert.Equal(BackupState.ACTIVE, state);
        }

        /// <summary>
        /// Verifies that the BackupState enumeration contains the PAUSED value.
        /// </summary>
        [Fact]
        public void BackupState_HasPausedValue()
        {
            var state = BackupState.PAUSED;

            Assert.Equal(BackupState.PAUSED, state);
        }

        /// <summary>
        /// Verifies that the BackupState enumeration contains the COMPLETED value.
        /// </summary>
        [Fact]
        public void BackupState_HasCompletedValue()
        {
            var state = BackupState.COMPLETED;

            Assert.Equal(BackupState.COMPLETED, state);
        }

        /// <summary>
        /// Verifies that the BackupState enumeration contains the ERROR value.
        /// </summary>
        [Fact]
        public void BackupState_HasErrorValue()
        {
            var state = BackupState.ERROR;

            Assert.Equal(BackupState.ERROR, state);
        }

        /// <summary>
        /// Verifies that the BackupState enumeration contains the PENDING value.
        /// </summary>
        [Fact]
        public void BackupState_HasPendingValue()
        {
            var state = BackupState.PENDING;

            Assert.Equal(BackupState.PENDING, state);
        }

        /// <summary>
        /// Verifies that all BackupState enumeration values are valid and defined.
        /// </summary>
        [Theory]
        [InlineData(BackupState.ACTIVE)]
        [InlineData(BackupState.PAUSED)]
        [InlineData(BackupState.COMPLETED)]
        [InlineData(BackupState.ERROR)]
        [InlineData(BackupState.PENDING)]
        public void BackupState_AllValuesAreValid(BackupState state)
        {
            Assert.True(System.Enum.IsDefined(typeof(BackupState), state));
        }
    }
}