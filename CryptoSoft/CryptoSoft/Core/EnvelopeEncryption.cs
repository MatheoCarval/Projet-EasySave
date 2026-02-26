using System.Security.Cryptography;
using CryptoSoft.Models;
using CryptoSoft.Utilities;

namespace CryptoSoft.Core;

/// <summary>
/// Implements hybrid (envelope) encryption:
///   • Generates a random AES-256 session key per operation
///   • Encrypts the session key with RSA-OAEP (SHA-256)
///   • AES-256-GCM encrypts the data
/// </summary>
public static class EnvelopeEncryption
{
    /// <summary>
    /// Encrypts a session key with an RSA public key using OAEP SHA-256.
    /// </summary>
    public static byte[] WrapSessionKey(RSA publicKey, byte[] sessionKey)
    {
        return publicKey.Encrypt(sessionKey, RSAEncryptionPadding.OaepSHA256);
    }

    /// <summary>
    /// Decrypts a session key with an RSA private key using OAEP SHA-256.
    /// </summary>
    public static byte[] UnwrapSessionKey(RSA privateKey, byte[] wrappedKey)
    {
        return privateKey.Decrypt(wrappedKey, RSAEncryptionPadding.OaepSHA256);
    }

    /// <summary>
    /// Generates a fresh AES-256 session key (32 bytes).
    /// </summary>
    public static byte[] GenerateSessionKey()
    {
        return SecureMemory.GenerateRandomBytes(Constants.AesKeySizeBytes);
    }

    /// <summary>
    /// Generates a fresh GCM nonce (12 bytes / 96 bits).
    /// </summary>
    public static byte[] GenerateNonce()
    {
        return SecureMemory.GenerateRandomBytes(Constants.GcmNonceSize);
    }

    /// <summary>
    /// Encrypts a single block of plaintext with AES-256-GCM.
    /// Returns (ciphertext, tag). Associated data is optional.
    /// </summary>
    public static (byte[] Ciphertext, byte[] Tag) AesGcmEncrypt(
        byte[] key, byte[] nonce, byte[] plaintext, byte[]? associatedData = null)
    {
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[Constants.GcmTagSize];

        using var aes = new AesGcm(key, Constants.GcmTagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag, associatedData);

        return (ciphertext, tag);
    }

    /// <summary>
    /// Decrypts a single block of ciphertext with AES-256-GCM.
    /// Throws <see cref="CryptographicException"/> if authentication fails.
    /// </summary>
    public static byte[] AesGcmDecrypt(
        byte[] key, byte[] nonce, byte[] ciphertext, byte[] tag, byte[]? associatedData = null)
    {
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(key, Constants.GcmTagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext, associatedData);

        return plaintext;
    }
}
