using Models.Enums;
using Xunit;

namespace EasySave.Tests.Models.Enums
{
    public class BackupTypeTests
    {
        [Fact]
        public void BackupType_HasCompleteValue()
        {
            // Arrange & Act
            var type = BackupType.COMPLETE;

            // Assert
            Assert.Equal(BackupType.COMPLETE, type);
        }

        [Fact]
        public void BackupType_HasDifferentialValue()
        {
            // Arrange & Act
            var type = BackupType.DIFFERENTIAL;

            // Assert
            Assert.Equal(BackupType.DIFFERENTIAL, type);
        }

        [Theory]
        [InlineData(BackupType.COMPLETE)]
        [InlineData(BackupType.DIFFERENTIAL)]
        public void BackupType_AllValuesAreValid(BackupType type)
        {
            // Assert
            Assert.True(System.Enum.IsDefined(typeof(BackupType), type));
        }
    }
}