using Utilities;
using Xunit;
using System.IO;

namespace EasySave.Tests.Helpers
{
    /// <summary>
    /// Unit tests for the FileSystemHelper class, verifying file size retrieval and error handling.
    /// </summary>
    public class FileSystemHelperTests
    {
        /// <summary>
        /// Verifies that GetFileSize returns the correct file size for an existing file.
        /// </summary>
        [Fact]
        public void GetFileSize_WithExistingFile_ReturnsSize()
        {
            var tempFile = Path.GetTempFileName();
            var testData = new byte[] { 1, 2, 3, 4, 5 };
            File.WriteAllBytes(tempFile, testData);

            try
            {
                var size = FileSystemHelper.GetFileSize(tempFile);

                Assert.Equal(testData.Length, size);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        /// <summary>
        /// Verifies that GetFileSize throws FileNotFoundException when given a path to a non-existent file.
        /// </summary>
        [Fact]
        public void GetFileSize_WithNonExistingFile_ThrowsFileNotFoundException()
        {
            var nonExistentFile = @"C:\NonExistent\File.txt";

            Assert.Throws<FileNotFoundException>(() => FileSystemHelper.GetFileSize(nonExistentFile));
        }
    }
}