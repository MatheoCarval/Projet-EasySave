using Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Services.Managers
{
    /// <summary>
    /// Manages file encryption by invoking the Cryptosoft executable for supported file extensions.
    /// </summary>
    public class CryptageManager
    {
        private readonly string _cryptosoftPath;
        private readonly HashSet<string> _encryptedExtensions;

        public CryptageManager(string cryptosoftPath, IEnumerable<string> encryptedExtensions)
        {
            _cryptosoftPath = string.IsNullOrWhiteSpace(cryptosoftPath) ? string.Empty : cryptosoftPath.Trim();
            _encryptedExtensions = new HashSet<string>(
                NormalizeExtensions(encryptedExtensions),
                StringComparer.OrdinalIgnoreCase
            );
        }

        /// <summary>
        /// Returns the time spent encrypting the file in milliseconds; 0 means no encryption performed.
        /// </summary>
        public long EncryptIfNeeded(string filePath, BackupJob job)
        {
            if (job == null) throw new ArgumentNullException(nameof(job));
            if (!job.EncryptFiles)
            {
                return 0;
            }

            if (!ShouldEncrypt(filePath))
            {
                return 0;
            }

            if (string.IsNullOrWhiteSpace(_cryptosoftPath))
            {
                return 0;
            }

            // TODO: Invoke Cryptosoft executable and measure elapsed time.
            return 0;
        }

        private bool ShouldEncrypt(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return false;
            }

            string extension = Path.GetExtension(filePath);
            if (string.IsNullOrWhiteSpace(extension))
            {
                return false;
            }

            return _encryptedExtensions.Contains(extension);
        }

        private static IEnumerable<string> NormalizeExtensions(IEnumerable<string> extensions)
        {
            if (extensions == null)
            {
                return Enumerable.Empty<string>();
            }

            return extensions
                .Where(ext => !string.IsNullOrWhiteSpace(ext))
                .Select(NormalizeExtension)
                .Where(ext => !string.IsNullOrWhiteSpace(ext));
        }

        private static string NormalizeExtension(string extension)
        {
            string trimmed = extension.Trim();
            if (!trimmed.StartsWith("."))
            {
                trimmed = "." + trimmed;
            }

            return trimmed.ToLowerInvariant();
        }
    }
}
