using Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private readonly string _publicKeyPath;
        private readonly HashSet<string> _encryptedExtensions;

        public CryptageManager(string cryptosoftPath, string publicKeyPath, IEnumerable<string> encryptedExtensions)
        {
            _cryptosoftPath = string.IsNullOrWhiteSpace(cryptosoftPath) ? string.Empty : cryptosoftPath.Trim();
            _publicKeyPath = string.IsNullOrWhiteSpace(publicKeyPath) ? string.Empty : publicKeyPath.Trim();
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

            if (string.IsNullOrWhiteSpace(_publicKeyPath))
            {
                return 0;
            }

            return EncryptFile(filePath);
        }

        /// <summary>
        /// Encrypts a file using CryptoSoft and returns the encryption time in milliseconds.
        /// Replaces the original file with the encrypted version (.enc).
        /// </summary>
        private long EncryptFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return 0;
            }

            if (!File.Exists(_cryptosoftPath))
            {
                throw new FileNotFoundException($"CryptoSoft executable not found: {_cryptosoftPath}");
            }

            if (!File.Exists(_publicKeyPath))
            {
                throw new FileNotFoundException($"Public key not found: {_publicKeyPath}");
            }

            string encryptedPath = filePath + ".enc";
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = _cryptosoftPath,
                    Arguments = $"encrypt --input \"{filePath}\" --output \"{encryptedPath}\" --pubkey \"{_publicKeyPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(processInfo))
                {
                    if (process == null)
                    {
                        throw new InvalidOperationException("Failed to start CryptoSoft process");
                    }

                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        string error = process.StandardError.ReadToEnd();
                        throw new InvalidOperationException($"CryptoSoft encryption failed with exit code {process.ExitCode}: {error}");
                    }
                }

                stopwatch.Stop();

                // Replace the original file with the encrypted version
                if (File.Exists(encryptedPath))
                {
                    File.Delete(filePath);
                    File.Move(encryptedPath, filePath);
                }
                else
                {
                    throw new FileNotFoundException($"Encrypted file not created: {encryptedPath}");
                }

                return stopwatch.ElapsedMilliseconds;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                // Cleanup encrypted file if it exists
                if (File.Exists(encryptedPath))
                {
                    try { File.Delete(encryptedPath); } catch { }
                }

                throw new InvalidOperationException($"Encryption failed for file: {filePath}", ex);
            }
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
