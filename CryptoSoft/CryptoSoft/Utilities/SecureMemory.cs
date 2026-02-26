using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace CryptoSoft.Utilities;

/// <summary>
/// Secure memory helpers: zeroing buffers, pinning, etc.
/// </summary>
public static class SecureMemory
{
    /// <summary>
    /// Cryptographically zeros a span of bytes, preventing compiler elision.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void SecureZero(Span<byte> buffer)
    {
        CryptographicOperations.ZeroMemory(buffer);
    }

    /// <summary>
    /// Cryptographically zeros a byte array.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void SecureZero(byte[] buffer)
    {
        CryptographicOperations.ZeroMemory(buffer);
    }

    /// <summary>
    /// Generates cryptographically strong random bytes.
    /// </summary>
    public static byte[] GenerateRandomBytes(int count)
    {
        var buffer = new byte[count];
        RandomNumberGenerator.Fill(buffer);
        return buffer;
    }

    /// <summary>
    /// Fills an existing span with cryptographically strong random bytes.
    /// </summary>
    public static void FillRandom(Span<byte> buffer)
    {
        RandomNumberGenerator.Fill(buffer);
    }
}
