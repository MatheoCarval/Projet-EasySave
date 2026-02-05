using Models.Enums;
using Xunit;

namespace EasySave.Tests.Models.Enums
{
    /// <summary>
    /// Unit tests for the BackupType enumeration, verifying all enum values are defined and accessible.
    /// </summary>
    public class BackupTypeTests
    {
        /// <summary>
        /// Verifies that the BackupType enumeration contains the COMPLETE value.
        /// </summary>
        [Fact]
        public void BackupType_HasCompleteValue()
        {
            var type = BackupType.COMPLETE;

            Assert.Equal(BackupType.COMPLETE, type);
        }

        /// <summary>
        /// Verifies that the BackupType enumeration contains the DIFFERENTIAL value.
        /// </summary>
        [Fact]
        public void BackupType_HasDifferentialValue()
        {
            var type = BackupType.DIFFERENTIAL;

            Assert.Equal(BackupType.DIFFERENTIAL, type);
        }

        /// <summary>
        /// Verifies that all BackupType enumeration values are valid and defined.
        /// </summary>
        [Theory]
        [InlineData(BackupType.COMPLETE)]
        [InlineData(BackupType.DIFFERENTIAL)]
        public void BackupType_AllValuesAreValid(BackupType type)
        {
            Assert.True(System.Enum.IsDefined(typeof(BackupType), type));
        }
    }
}