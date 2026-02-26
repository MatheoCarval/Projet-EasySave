namespace CryptoSoft.Models;

/// <summary>
/// Constants used throughout CryptoSoft.
/// </summary>
public static class Constants
{
    // ── File format ──────────────────────────────────────────────────
    /// <summary>Magic bytes identifying a CryptoSoft encrypted file: "CSFT".</summary>
    public static readonly byte[] MagicBytes = "CSFT"u8.ToArray();

    /// <summary>Current file format version.</summary>
    public const ushort FormatVersion = 1;

    // ── AES-256-GCM ─────────────────────────────────────────────────
    public const int AesKeySize = 256;          // bits
    public const int AesKeySizeBytes = 32;      // bytes
    public const int GcmNonceSize = 12;         // bytes (96 bits)
    public const int GcmTagSize = 16;           // bytes (128 bits)

    // ── RSA ──────────────────────────────────────────────────────────
    public const int RsaKeySize = 4096;         // bits

    // ── Streaming ────────────────────────────────────────────────────
    /// <summary>64 KiB plaintext chunks for streaming encryption.</summary>
    public const int ChunkSize = 64 * 1024;

    // ── Algorithms identifiers (stored in header) ────────────────────
    public const byte AlgorithmRsaOaepAes256Gcm = 0x01;
}
