using Utilities;
using Xunit;
using System.IO;

namespace EasySave.Tests.Helpers
{
    public class FileSystemHelperTests
    {
        [Fact]
        public void GetFileSize_WithExistingFile_ReturnsSize()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            var testData = new byte[] { 1, 2, 3, 4, 5 };
            File.WriteAllBytes(tempFile, testData);

            try
            {
                // Act
                var size = FileSystemHelper.GetFileSize(tempFile);

                // Assert
                Assert.Equal(testData.Length, size);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void GetFileSize_WithNonExistingFile_ThrowsFileNotFoundException()
        {
            // Arrange
            var nonExistentFile = @"C:\NonExistent\File.txt";

            // Act & Assert
            Assert.Throws<FileNotFoundException>(() => FileSystemHelper.GetFileSize(nonExistentFile));
        }
    }
}