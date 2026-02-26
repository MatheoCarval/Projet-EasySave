using System.IO.Compression;
using System.Security.Cryptography;
using CryptoSoft.Core;

namespace CryptoSoft.Services;

/// <summary>
/// High-level file and directory operations: encrypt/decrypt files,
/// handle directories (with optional compression), and stdin/stdout piping.
/// </summary>
public static class FileStreamProcessor
{
    // ─── Single file ────────────────────────────────────────────────

    /// <summary>
    /// Encrypts a single file.
    /// </summary>
    public static void EncryptFile(string inputPath, string outputPath, RSA publicKey)
    {
        using var input  = new FileStream(inputPath, FileMode.Open, FileAccess.Read,
            FileShare.Read, bufferSize: 81920, FileOptions.SequentialScan);
        using var output = new FileStream(outputPath, FileMode.Create, FileAccess.Write,
            FileShare.None, bufferSize: 81920, FileOptions.SequentialScan);

        CryptoEngine.Encrypt(input, output, publicKey);
    }

    /// <summary>
    /// Decrypts a single file.
    /// </summary>
    public static void DecryptFile(string inputPath, string outputPath, RSA privateKey)
    {
        using var input  = new FileStream(inputPath, FileMode.Open, FileAccess.Read,
            FileShare.Read, bufferSize: 81920, FileOptions.SequentialScan);
        using var output = new FileStream(outputPath, FileMode.Create, FileAccess.Write,
            FileShare.None, bufferSize: 81920, FileOptions.SequentialScan);

        CryptoEngine.Decrypt(input, output, privateKey);
    }

    // ─── Directory (tar.gz → encrypt) ───────────────────────────────

    /// <summary>
    /// Compresses a directory into a tar.gz archive in-memory,
    /// then encrypts the archive to the output file.
    /// Uses a temp file to avoid holding entire archive in RAM.
    /// </summary>
    public static void EncryptDirectory(string directoryPath, string outputPath, RSA publicKey)
    {
        string tempArchive = Path.GetTempFileName();
        try
        {
            // Compress directory to temp tar.gz
            CompressDirectory(directoryPath, tempArchive);

            // Encrypt the archive
            EncryptFile(tempArchive, outputPath, publicKey);
        }
        finally
        {
            SecureDeleteFile(tempArchive);
        }
    }

    /// <summary>
    /// Decrypts and decompresses a directory archive.
    /// </summary>
    public static void DecryptDirectory(string inputPath, string outputDirectory, RSA privateKey)
    {
        string tempArchive = Path.GetTempFileName();
        try
        {
            // Decrypt to temp archive
            DecryptFile(inputPath, tempArchive, privateKey);

            // Extract
            DecompressDirectory(tempArchive, outputDirectory);
        }
        finally
        {
            SecureDeleteFile(tempArchive);
        }
    }

    // ─── Stdin / Stdout piping ──────────────────────────────────────

    /// <summary>
    /// Encrypts from stdin to stdout (for pipeline integration).
    /// </summary>
    public static void EncryptStdio(RSA publicKey)
    {
        using var input  = Console.OpenStandardInput();
        using var output = Console.OpenStandardOutput();
        CryptoEngine.Encrypt(input, output, publicKey);
    }

    /// <summary>
    /// Decrypts from stdin to stdout.
    /// </summary>
    public static void DecryptStdio(RSA privateKey)
    {
        using var input  = Console.OpenStandardInput();
        using var output = Console.OpenStandardOutput();
        CryptoEngine.Decrypt(input, output, privateKey);
    }

    // ─── Archive helpers ────────────────────────────────────────────

    private static void CompressDirectory(string directoryPath, string outputArchive)
    {
        using var fileStream = new FileStream(outputArchive, FileMode.Create,
            FileAccess.Write, FileShare.None, bufferSize: 81920);
        using var gzipStream = new GZipStream(fileStream, CompressionLevel.Optimal);

        // Simple tar-like format: write each file with a header
        var basePath = Path.GetFullPath(directoryPath);
        foreach (var filePath in Directory.EnumerateFiles(basePath, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(basePath, filePath);
            WriteArchiveEntry(gzipStream, relativePath, filePath);
        }

        // Write end-of-archive marker
        WriteArchiveEndMarker(gzipStream);
    }

    private static void DecompressDirectory(string archivePath, string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);

        using var fileStream = new FileStream(archivePath, FileMode.Open,
            FileAccess.Read, FileShare.Read, bufferSize: 81920);
        using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);

        while (true)
        {
            var entry = ReadArchiveEntry(gzipStream, outputDirectory);
            if (entry == null) break;
        }
    }

    /// <summary>
    /// Simple archive entry format:
    ///   [4 bytes path length (LE)][UTF-8 path][8 bytes file length (LE)][file data]
    /// End marker: path length == 0
    /// </summary>
    private static void WriteArchiveEntry(Stream archive, string relativePath, string filePath)
    {
        byte[] pathBytes = System.Text.Encoding.UTF8.GetBytes(relativePath);
        Span<byte> buf = stackalloc byte[8];

        // Path length + path
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(buf, pathBytes.Length);
        archive.Write(buf[..4]);
        archive.Write(pathBytes);

        // File length + data
        var fileInfo = new FileInfo(filePath);
        System.Buffers.Binary.BinaryPrimitives.WriteInt64LittleEndian(buf, fileInfo.Length);
        archive.Write(buf);

        using var fs = fileInfo.OpenRead();
        fs.CopyTo(archive, bufferSize: 81920);
    }

    private static void WriteArchiveEndMarker(Stream archive)
    {
        Span<byte> zero = stackalloc byte[4];
        zero.Clear();
        archive.Write(zero);
    }

    private static string? ReadArchiveEntry(Stream archive, string outputDirectory)
    {
        Span<byte> buf = stackalloc byte[8];

        // Read path length
        if (!TryReadExact(archive, buf[..4])) return null;
        int pathLen = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(buf);
        if (pathLen == 0) return null;

        // Read path
        byte[] pathBytes = new byte[pathLen];
        ReadExact(archive, pathBytes);
        string relativePath = System.Text.Encoding.UTF8.GetString(pathBytes);

        // Prevent path traversal attacks
        string fullPath = Path.GetFullPath(Path.Combine(outputDirectory, relativePath));
        if (!fullPath.StartsWith(Path.GetFullPath(outputDirectory)))
            throw new InvalidOperationException("Path traversal detected in archive.");

        // Read file length
        ReadExact(archive, buf);
        long fileLen = System.Buffers.Binary.BinaryPrimitives.ReadInt64LittleEndian(buf);

        // Extract file
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        using var outFile = new FileStream(fullPath, FileMode.Create, FileAccess.Write);

        byte[] copyBuf = new byte[81920];
        long remaining = fileLen;
        while (remaining > 0)
        {
            int toRead = (int)Math.Min(remaining, copyBuf.Length);
            int read = archive.Read(copyBuf, 0, toRead);
            if (read == 0) throw new EndOfStreamException("Truncated archive.");
            outFile.Write(copyBuf, 0, read);
            remaining -= read;
        }

        return relativePath;
    }

    private static bool TryReadExact(Stream stream, Span<byte> buffer)
    {
        int total = 0;
        while (total < buffer.Length)
        {
            int n = stream.Read(buffer[total..]);
            if (n == 0) return total > 0
                ? throw new EndOfStreamException("Truncated archive header.")
                : false;
            total += n;
        }
        return true;
    }

    private static void ReadExact(Stream stream, Span<byte> buffer)
    {
        int total = 0;
        while (total < buffer.Length)
        {
            int n = stream.Read(buffer[total..]);
            if (n == 0) throw new EndOfStreamException();
            total += n;
        }
    }

    /// <summary>
    /// Securely deletes a temp file by overwriting with zeros before removal.
    /// </summary>
    private static void SecureDeleteFile(string path)
    {
        try
        {
            if (!File.Exists(path)) return;

            var info = new FileInfo(path);
            long length = info.Length;

            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Write))
            {
                byte[] zeros = new byte[Math.Min(81920, length)];
                long remaining = length;
                while (remaining > 0)
                {
                    int toWrite = (int)Math.Min(remaining, zeros.Length);
                    fs.Write(zeros, 0, toWrite);
                    remaining -= toWrite;
                }
                fs.Flush(flushToDisk: true);
            }

            File.Delete(path);
        }
        catch
        {
            // Best effort – don't leak exceptions from cleanup
            try { File.Delete(path); } catch { }
        }
    }
}
