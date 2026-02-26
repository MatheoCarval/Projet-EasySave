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
        // CryptoSoft is mono-instance: only one process can run at a time across all parallel jobs.
        private static readonly SemaphoreSlim _cryptosoftLock = new(1, 1);

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

            if (!ShouldEncrypt(filePath, job))
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

            _cryptosoftLock.Wait();
            try
            {
                return EncryptFile(filePath);
            }
            finally
            {
                _cryptosoftLock.Release();
            }
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
                    RedirectStandardOutput = false, // not needed — don't redirect to avoid deadlock
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(processInfo))
                {
                    if (process == null)
                    {
                        throw new InvalidOperationException("Failed to start CryptoSoft process");
                    }

                    // Read stderr BEFORE WaitForExit: avoids deadlock when the stderr pipe buffer fills up.
                    string stderr = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        throw new InvalidOperationException($"CryptoSoft encryption failed (exit {process.ExitCode}): {stderr.Trim()}");
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

        private bool ShouldEncrypt(string filePath, BackupJob job)
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

            // Check global extensions (from config)
            if (_encryptedExtensions.Contains(extension))
            {
                return true;
            }

            // Check per-job extensions
            if (job.EncryptedExtensions?.Count > 0)
            {
                var jobExtensions = new HashSet<string>(
                    NormalizeExtensions(job.EncryptedExtensions),
                    StringComparer.OrdinalIgnoreCase);
                return jobExtensions.Contains(extension);
            }

            return false;
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
