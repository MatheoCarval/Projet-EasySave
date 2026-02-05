using System.IO;

namespace Utilities
{
    /// <summary>
    /// Provides utility methods for file system operations including file and directory size calculations, copying files with progress reporting, and directory management.
    /// </summary>
    public static class FileSystemHelper
    {
        /// <summary>
        /// Retrieves the size in bytes of the specified file. Throws ArgumentException if path is null or empty, and FileNotFoundException if file does not exist.
        /// </summary>
        public static long GetFileSize(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Le chemin du fichier ne peut pas être vide.", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException("Le fichier n'existe pas.", filePath);

            var fileInfo = new FileInfo(filePath);
            return fileInfo.Length;
        }

        /// <summary>
        /// Counts the total number of files in the specified directory and all subdirectories recursively.
        /// </summary>
        public static long GetFileCount(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
                throw new ArgumentException("Le chemin ne peut pas être vide.", nameof(directoryPath));

            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException($"Le dossier n'existe pas : {directoryPath}");

            return Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories).LongCount();
        }

        /// <summary>
        /// Calculates the total size in bytes of all files in the specified directory and all subdirectories recursively.
        /// </summary>
        public static long GetDirectorySize(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
                throw new ArgumentException("Le chemin ne peut pas être vide.", nameof(directoryPath));

            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException($"Le dossier n'existe pas : {directoryPath}");

            return Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories)
                            .Sum(file => new FileInfo(file).Length);
        }

        /// <summary>
        /// Creates the specified directory if it does not already exist. Throws ArgumentException if the path is null or empty.
        /// </summary>
        public static void EnsureDirectoryExists(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
                throw new ArgumentException("Le chemin ne peut pas être vide.", nameof(directoryPath));

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        /// <summary>
        /// Copies a file from source to destination with optional progress reporting. Supports custom buffer size for performance tuning. Creates destination directory if needed and handles empty files.
        /// </summary>
        public static void CopyWithProgress(
            string sourcePath,
            string destinationPath,
            IProgress<double>? progress = null,
            int bufferSize = 81920)
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
                throw new ArgumentException("Le chemin source ne peut pas être vide.", nameof(sourcePath));

            if (string.IsNullOrWhiteSpace(destinationPath))
                throw new ArgumentException("Le chemin destination ne peut pas être vide.", nameof(destinationPath));

            if (!File.Exists(sourcePath))
                throw new FileNotFoundException("Le fichier source n'existe pas.", sourcePath);

            string? destinationDirectory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(destinationDirectory))
            {
                EnsureDirectoryExists(destinationDirectory);
            }

            var fileInfo = new FileInfo(sourcePath);
            long totalBytes = fileInfo.Length;

            if (totalBytes == 0)
            {
                File.Create(destinationPath).Dispose();
                progress?.Report(100);
                return;
            }

            long totalBytesCopied = 0;
            byte[] buffer = new byte[bufferSize];

            using (var sourceStream = new FileStream(
                sourcePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize,
                FileOptions.SequentialScan))
            using (var destinationStream = new FileStream(
                destinationPath,
                FileMode.Create,
                FileAccess.Write))
            {
                int bytesRead;
                while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    destinationStream.Write(buffer, 0, bytesRead);
                    totalBytesCopied += bytesRead;
                    double percentage = (double)totalBytesCopied / totalBytes * 100;

                    progress?.Report(percentage);
                }
            }
        }
    }
}