using Utilities;
using Xunit;
using System.IO;

namespace EasySave.Tests.Helpers
{
    public class PathValidatorTests
    {
        [Fact]
        public void PathExists_WithExistingPath_ReturnsTrue()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();

            try
            {
                // Act
                var result = PathValidator.PathExists(tempFile);

                // Assert
                Assert.True(result);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void PathExists_WithNonExistingPath_ReturnsFalse()
        {
            // Arrange
            var nonExistentPath = @"C:\NonExistentPath\File.txt";

            // Act
            var result = PathValidator.PathExists(nonExistentPath);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsDirectory_WithDirectoryPath_ReturnsTrue()
        {
            // Arrange
            var tempDir = Path.GetTempPath();

            // Act
            var result = PathValidator.IsDirectory(tempDir);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsDirectory_WithFilePath_ReturnsFalse()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();

            try
            {
                // Act
                var result = PathValidator.IsDirectory(tempFile);

                // Assert
                Assert.False(result);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Theory]
        [InlineData(@"C:\Test\Path", @"\\?\C:\Test\Path")]
        [InlineData(@"D:\Folder\File.txt", @"\\?\D:\Folder\File.txt")]
        public void ToUncPath_ConvertsPathCorrectly(string input, string expected)
        {
            // Act
            var result = PathValidator.ToUncPath(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ToUncPath_WithAlreadyUncPath_ReturnsUnchanged()
        {
            // Arrange
            var uncPath = @"\\?\C:\Test\Path";

            // Act
            var result = PathValidator.ToUncPath(uncPath);

            // Assert
            Assert.Equal(uncPath, result);
        }
    }
}