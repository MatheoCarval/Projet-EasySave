using System.Buffers.Binary;
using System.Security.Cryptography;
using CryptoSoft.Models;
using CryptoSoft.Utilities;

namespace CryptoSoft.Core;

/// <summary>
/// Streaming encryption / decryption engine.
/// Processes data in 64 KiB chunks so that files > 10 GB can be handled
/// without loading everything into memory.
///
/// Each chunk is independently AES-256-GCM encrypted with a per-chunk nonce
/// derived from the base nonce + chunk counter to avoid nonce reuse.
/// </summary>
public static class CryptoEngine
{
    // ─── Encrypt ────────────────────────────────────────────────────

    /// <summary>
    /// Encrypts <paramref name="inputStream"/> into <paramref name="outputStream"/>
    /// using hybrid RSA-OAEP + AES-256-GCM envelope encryption.
    /// </summary>
    public static void Encrypt(Stream inputStream, Stream outputStream, RSA publicKey)
    {
        // 1. Generate ephemeral AES-256 session key + base nonce
        byte[] sessionKey = EnvelopeEncryption.GenerateSessionKey();
        byte[] baseNonce  = EnvelopeEncryption.GenerateNonce();

        try
        {
            // 2. Wrap session key with RSA public key
            byte[] wrappedKey = EnvelopeEncryption.WrapSessionKey(publicKey, sessionKey);

            // 3. Write file header
            var header = new CryptoFileHeader
            {
                Version    = Constants.FormatVersion,
                Algorithm  = Constants.AlgorithmRsaOaepAes256Gcm,
                Nonce      = baseNonce,
                WrappedKey = wrappedKey
            };
            header.WriteTo(outputStream);

            // 4. Stream-encrypt in chunks
            EncryptChunks(inputStream, outputStream, sessionKey, baseNonce);
        }
        finally
        {
            SecureMemory.SecureZero(sessionKey);
        }
    }

    // ─── Decrypt ────────────────────────────────────────────────────

    /// <summary>
    /// Decrypts a CryptoSoft file from <paramref name="inputStream"/>
    /// into <paramref name="outputStream"/>.
    /// </summary>
    public static void Decrypt(Stream inputStream, Stream outputStream, RSA privateKey)
    {
        // 1. Read & validate header
        var header = CryptoFileHeader.ReadFrom(inputStream);

        if (header.Algorithm != Constants.AlgorithmRsaOaepAes256Gcm)
            throw new CryptographicException(
                $"Unsupported algorithm 0x{header.Algorithm:X2}.");

        // 2. Unwrap session key
        byte[] sessionKey = EnvelopeEncryption.UnwrapSessionKey(privateKey, header.WrappedKey);

        try
        {
            // 3. Stream-decrypt chunks
            DecryptChunks(inputStream, outputStream, sessionKey, header.Nonce);
        }
        finally
        {
            SecureMemory.SecureZero(sessionKey);
        }
    }

    // ─── Chunk-level streaming ──────────────────────────────────────

    /// <summary>
    /// Encrypts the input in 64 KiB chunks.
    /// Each chunk format: [4-byte LE ciphertext length][ciphertext][16-byte GCM tag]
    /// End-of-stream sentinel: [4-byte zero length]
    /// </summary>
    private static void EncryptChunks(
        Stream input, Stream output, byte[] sessionKey, byte[] baseNonce)
    {
        byte[] plainBuffer = new byte[Constants.ChunkSize];
        byte[] chunkNonce  = new byte[Constants.GcmNonceSize];
        Span<byte> lenBuf  = stackalloc byte[4];

        uint chunkIndex = 0;

        try
        {
            while (true)
            {
                int bytesRead = ReadFull(input, plainBuffer);
                if (bytesRead == 0)
                    break;

                // Derive per-chunk nonce: baseNonce XOR chunkIndex (LE in last 4 bytes)
                DeriveChunkNonce(baseNonce, chunkIndex, chunkNonce);

                // Encrypt chunk
                byte[] plainSlice = plainBuffer.AsSpan(0, bytesRead).ToArray();
                var (ciphertext, tag) = EnvelopeEncryption.AesGcmEncrypt(
                    sessionKey, chunkNonce, plainSlice);

                // Write: length + ciphertext + tag
                BinaryPrimitives.WriteInt32LittleEndian(lenBuf, ciphertext.Length);
                output.Write(lenBuf);
                output.Write(ciphertext);
                output.Write(tag);

                SecureMemory.SecureZero(plainSlice);

                chunkIndex++;
            }

            // End-of-stream sentinel
            BinaryPrimitives.WriteInt32LittleEndian(lenBuf, 0);
            output.Write(lenBuf);
        }
        finally
        {
            SecureMemory.SecureZero(plainBuffer);
            SecureMemory.SecureZero(chunkNonce);
        }
    }

    /// <summary>
    /// Decrypts chunks written by <see cref="EncryptChunks"/>.
    /// </summary>
    private static void DecryptChunks(
        Stream input, Stream output, byte[] sessionKey, byte[] baseNonce)
    {
        byte[] chunkNonce = new byte[Constants.GcmNonceSize];
        Span<byte> lenBuf = stackalloc byte[4];

        uint chunkIndex = 0;

        try
        {
            while (true)
            {
                // Read chunk ciphertext length
                ReadExact(input, lenBuf);
                int cipherLen = BinaryPrimitives.ReadInt32LittleEndian(lenBuf);

                if (cipherLen == 0)
                    break; // EOS sentinel

                if (cipherLen < 0 || cipherLen > Constants.ChunkSize + 256)
                    throw new InvalidDataException(
                        $"Invalid chunk size {cipherLen} at chunk {chunkIndex}.");

                // Read ciphertext + tag
                byte[] ciphertext = new byte[cipherLen];
                ReadExact(input, ciphertext);

                byte[] tag = new byte[Constants.GcmTagSize];
                ReadExact(input, tag);

                // Derive per-chunk nonce
                DeriveChunkNonce(baseNonce, chunkIndex, chunkNonce);

                // Decrypt & authenticate
                byte[] plaintext;
                try
                {
                    plaintext = EnvelopeEncryption.AesGcmDecrypt(
                        sessionKey, chunkNonce, ciphertext, tag);
                }
                catch (CryptographicException)
                {
                    throw new CryptographicException(
                        $"Authentication failed at chunk {chunkIndex}. " +
                        "The file may have been tampered with or the wrong key was used.");
                }

                output.Write(plaintext);
                SecureMemory.SecureZero(plaintext);

                chunkIndex++;
            }
        }
        finally
        {
            SecureMemory.SecureZero(chunkNonce);
        }
    }

    // ─── Nonce derivation ───────────────────────────────────────────

    /// <summary>
    /// Derives a unique per-chunk nonce by XORing the chunk counter (LE)
    /// into the last 4 bytes of the base nonce. This guarantees nonce
    /// uniqueness without additional randomness.
    /// </summary>
    private static void DeriveChunkNonce(byte[] baseNonce, uint chunkIndex, byte[] dest)
    {
        Buffer.BlockCopy(baseNonce, 0, dest, 0, Constants.GcmNonceSize);

        Span<byte> counterBytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(counterBytes, chunkIndex);

        // XOR into last 4 bytes
        int offset = Constants.GcmNonceSize - 4;
        dest[offset + 0] ^= counterBytes[0];
        dest[offset + 1] ^= counterBytes[1];
        dest[offset + 2] ^= counterBytes[2];
        dest[offset + 3] ^= counterBytes[3];
    }

    // ─── IO helpers ─────────────────────────────────────────────────

    /// <summary>
    /// Reads up to buffer.Length bytes, returning the actual count. Handles partial reads.
    /// </summary>
    private static int ReadFull(Stream stream, byte[] buffer)
    {
        int totalRead = 0;
        while (totalRead < buffer.Length)
        {
            int n = stream.Read(buffer, totalRead, buffer.Length - totalRead);
            if (n == 0) break;
            totalRead += n;
        }
        return totalRead;
    }

    private static void ReadExact(Stream stream, Span<byte> buffer)
    {
        int totalRead = 0;
        while (totalRead < buffer.Length)
        {
            int n = stream.Read(buffer[totalRead..]);
            if (n == 0)
                throw new EndOfStreamException("Unexpected end of encrypted file.");
            totalRead += n;
        }
    }
}
