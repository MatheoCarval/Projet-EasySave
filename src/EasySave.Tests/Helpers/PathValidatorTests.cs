using Utilities;
using Xunit;
using System.IO;
using System.Runtime.InteropServices;

namespace EasySave.Tests.Helpers
{
    /// <summary>
    /// Unit tests for the PathValidator utility class, verifying path existence checks, directory detection, and UNC path conversion functionality.
    /// </summary>
    public class PathValidatorTests
    {
        /// <summary>
        /// Verifies that PathExists returns true for a path that exists in the file system.
        /// </summary>
        [Fact]
        public void PathExists_WithExistingPath_ReturnsTrue()
        {
            var tempFile = Path.GetTempFileName();

            try
            {
                var result = PathValidator.PathExists(tempFile);

                Assert.True(result);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        /// <summary>
        /// Verifies that PathExists returns false for a path that does not exist in the file system.
        /// </summary>
        [Fact]
        public void PathExists_WithNonExistingPath_ReturnsFalse()
        {
            var nonExistentPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
                ? @"C:\NonExistentPath\File.txt" 
                : "/nonexistent/path/file.txt";

            var result = PathValidator.PathExists(nonExistentPath);

            Assert.False(result);
        }

        /// <summary>
        /// Verifies that IsDirectory returns true when given a path that points to a directory.
        /// </summary>
        [Fact]
        public void IsDirectory_WithDirectoryPath_ReturnsTrue()
        {
            var tempDir = Path.GetTempPath();

            var result = PathValidator.IsDirectory(tempDir);

            Assert.True(result);
        }

        /// <summary>
        /// Verifies that IsDirectory returns false when given a path that points to a file rather than a directory.
        /// </summary>
        [Fact]
        public void IsDirectory_WithFilePath_ReturnsFalse()
        {
            var tempFile = Path.GetTempFileName();

            try
            {
                var result = PathValidator.IsDirectory(tempFile);

                Assert.False(result);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        /// <summary>
        /// Verifies that ToUncPath correctly converts standard Windows drive paths to their equivalent UNC long path format.
        /// </summary>
        [SkippableFact]
        public void ToUncPath_ConvertsPathCorrectly()
        {
            Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), "UNC paths are Windows-specific");

            var input1 = @"C:\Test\Path";
            var expected1 = @"\\?\C:\Test\Path";
            var input2 = @"D:\Folder\File.txt";
            var expected2 = @"\\?\D:\Folder\File.txt";

            var result1 = PathValidator.ToUncPath(input1);
            var result2 = PathValidator.ToUncPath(input2);

            Assert.Equal(expected1, result1);
            Assert.Equal(expected2, result2);
        }

        /// <summary>
        /// Verifies that ToUncPath returns an unchanged path when the input is already in UNC long path format.
        /// </summary>
        [SkippableFact]
        public void ToUncPath_WithAlreadyUncPath_ReturnsUnchanged()
        {
            Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), "UNC paths are Windows-specific");

            var uncPath = @"\\?\C:\Test\Path";

            var result = PathValidator.ToUncPath(uncPath);

            Assert.Equal(uncPath, result);
        }
    }
}