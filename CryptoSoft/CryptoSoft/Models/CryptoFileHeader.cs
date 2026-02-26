using System.Buffers.Binary;

namespace CryptoSoft.Models;

/// <summary>
/// Defines the binary header of a CryptoSoft encrypted file (.enc).
///
/// Layout (all multi-byte integers are little-endian):
/// ┌───────────────────────────────────────────────────────┐
/// │ Offset │ Size        │ Field                          │
/// ├────────┼─────────────┼────────────────────────────────┤
/// │  0     │ 4           │ Magic bytes "CSFT"             │
/// │  4     │ 2           │ Format version (uint16)        │
/// │  6     │ 1           │ Algorithm identifier           │
/// │  7     │ 2           │ Wrapped key length (uint16)    │
/// │  9     │ 12          │ Nonce / IV (96 bits)           │
/// │ 21     │ WKL         │ Wrapped session key            │
/// │ 21+WKL │ …           │ Encrypted data chunks          │
/// └───────────────────────────────────────────────────────┘
///
/// Each encrypted chunk:
///   [4 bytes chunk-ciphertext-length (LE)] [ciphertext] [16 bytes GCM tag]
///   A zero-length chunk (length == 0) signals end-of-stream.
/// </summary>
public sealed class CryptoFileHeader
{
    public ushort Version { get; init; } = Constants.FormatVersion;
    public byte Algorithm { get; init; } = Constants.AlgorithmRsaOaepAes256Gcm;
    public byte[] Nonce { get; init; } = Array.Empty<byte>();
    public byte[] WrappedKey { get; init; } = Array.Empty<byte>();

    // ── Serialisation ───────────────────────────────────────────────

    /// <summary>
    /// Writes the header to a stream.
    /// </summary>
    public void WriteTo(Stream stream)
    {
        Span<byte> buf = stackalloc byte[4];

        // Magic
        stream.Write(Constants.MagicBytes);

        // Version (LE 16)
        BinaryPrimitives.WriteUInt16LittleEndian(buf, Version);
        stream.Write(buf[..2]);

        // Algorithm
        stream.WriteByte(Algorithm);

        // Wrapped key length + data
        BinaryPrimitives.WriteUInt16LittleEndian(buf, (ushort)WrappedKey.Length);
        stream.Write(buf[..2]);

        // Nonce
        stream.Write(Nonce);

        // Wrapped session key
        stream.Write(WrappedKey);
    }

    /// <summary>
    /// Reads and validates the header from a stream.
    /// </summary>
    public static CryptoFileHeader ReadFrom(Stream stream)
    {
        Span<byte> magic = stackalloc byte[4];
        ReadExact(stream, magic);
        if (!magic.SequenceEqual(Constants.MagicBytes))
            throw new InvalidDataException("Not a valid CryptoSoft file (bad magic bytes).");

        Span<byte> vBuf = stackalloc byte[2];
        ReadExact(stream, vBuf);
        ushort version = BinaryPrimitives.ReadUInt16LittleEndian(vBuf);
        if (version > Constants.FormatVersion)
            throw new InvalidDataException(
                $"Unsupported format version {version}. Maximum supported: {Constants.FormatVersion}.");

        int algorithm = stream.ReadByte();
        if (algorithm < 0)
            throw new EndOfStreamException("Unexpected end of file reading algorithm byte.");

        Span<byte> wklBuf = stackalloc byte[2];
        ReadExact(stream, wklBuf);
        ushort wrappedKeyLen = BinaryPrimitives.ReadUInt16LittleEndian(wklBuf);

        byte[] nonce = new byte[Constants.GcmNonceSize];
        ReadExact(stream, nonce);

        byte[] wrappedKey = new byte[wrappedKeyLen];
        ReadExact(stream, wrappedKey);

        return new CryptoFileHeader
        {
            Version = version,
            Algorithm = (byte)algorithm,
            Nonce = nonce,
            WrappedKey = wrappedKey
        };
    }

    /// <summary>
    /// Returns the total size of the serialized header in bytes.
    /// </summary>
    public int SerializedSize =>
        4       // magic
        + 2     // version
        + 1     // algorithm
        + 2     // wrapped key length
        + Constants.GcmNonceSize
        + WrappedKey.Length;

    // ── Helpers ─────────────────────────────────────────────────────

    private static void ReadExact(Stream stream, Span<byte> buffer)
    {
        int totalRead = 0;
        while (totalRead < buffer.Length)
        {
            int n = stream.Read(buffer[totalRead..]);
            if (n == 0)
                throw new EndOfStreamException("Unexpected end of file.");
            totalRead += n;
        }
    }
}
