using Utilities;
using Xunit;
using System.IO;
using System.Runtime.InteropServices;

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
            var nonExistentPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
                ? @"C:\NonExistentPath\File.txt" 
                : "/nonexistent/path/file.txt";

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

        [SkippableFact]
        public void ToUncPath_ConvertsPathCorrectly()
        {
            Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), "UNC paths are Windows-specific");

            // Arrange
            var input1 = @"C:\Test\Path";
            var expected1 = @"\\localhost\C$\Test\Path";
            var input2 = @"D:\Folder\File.txt";
            var expected2 = @"\\localhost\D$\Folder\File.txt";

            // Act
            var result1 = PathValidator.ToUncPath(input1);
            var result2 = PathValidator.ToUncPath(input2);

            // Assert
            Assert.Equal(expected1, result1);
            Assert.Equal(expected2, result2);
        }

        [SkippableFact]
        public void ToUncPath_WithAlreadyUncPath_ReturnsUnchanged()
        {
            Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), "UNC paths are Windows-specific");

            // Arrange
            var uncPath = @"\\?\C:\Test\Path";

            // Act
            var result = PathValidator.ToUncPath(uncPath);

            // Assert
            Assert.Equal(uncPath, result);
        }
    }
}