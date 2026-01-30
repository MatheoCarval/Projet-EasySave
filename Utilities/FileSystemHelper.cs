using System.IO;

namespace Utilities
{
    public static class FileSystemHelper
    {
        public static long GetFileSize(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Le chemin du fichier ne peut pas être vide.", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException("Le fichier n'existe pas.", filePath);

            var fileInfo = new FileInfo(filePath);
            return fileInfo.Length;
        }

        public static long GetFileCount(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
                throw new ArgumentException("Le chemin ne peut pas être vide.", nameof(directoryPath));

            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException($"Le dossier n'existe pas : {directoryPath}");

            return Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories).LongCount();
        }

        public static long GetDirectorySize(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
                throw new ArgumentException("Le chemin ne peut pas être vide.", nameof(directoryPath));

            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException($"Le dossier n'existe pas : {directoryPath}");

            return Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories)
                            .Sum(file => new FileInfo(file).Length);
        }

        public static void EnsureDirectoryExists(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
                throw new ArgumentException("Le chemin ne peut pas être vide.", nameof(directoryPath));

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        public static void CopyWithProgress(
            string sourcePath,
            string destinationPath,
            IProgress<double>? progress = null,
            int bufferSize = 81920)
        {
            // 1. Validations
            if (string.IsNullOrWhiteSpace(sourcePath))
                throw new ArgumentException("Le chemin source ne peut pas être vide.", nameof(sourcePath));

            if (string.IsNullOrWhiteSpace(destinationPath))
                throw new ArgumentException("Le chemin destination ne peut pas être vide.", nameof(destinationPath));

            if (!File.Exists(sourcePath))
                throw new FileNotFoundException("Le fichier source n'existe pas.", sourcePath);

            // 2. S'assurer que le dossier de destination existe
            string? destinationDirectory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(destinationDirectory))
            {
                EnsureDirectoryExists(destinationDirectory);
            }

            var fileInfo = new FileInfo(sourcePath);
            long totalBytes = fileInfo.Length;

            // 3. Vérifier si le fichier est vide
            if (totalBytes == 0)
            {
                File.Create(destinationPath).Dispose();
                progress?.Report(100);
                return;
            }

            long totalBytesCopied = 0;
            byte[] buffer = new byte[bufferSize];

            // 4. Optimisations FileStream
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