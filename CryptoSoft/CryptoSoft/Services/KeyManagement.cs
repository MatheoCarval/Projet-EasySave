using System.Security.Cryptography;
using System.Text;
using CryptoSoft.Models;

namespace CryptoSoft.Services;

/// <summary>
/// Generates, loads, and exports RSA-4096 key pairs in PEM format.
/// Keys are never logged; private key material is zeroed after use where feasible.
/// </summary>
public static class KeyManagement
{
    // ── Key generation ──────────────────────────────────────────────

    /// <summary>
    /// Generates an RSA-4096 key pair and writes PEM files.
    /// The private key file permissions are set to owner-only when possible.
    /// </summary>
    public static void GenerateKeyPair(string privateKeyPath, string publicKeyPath)
    {
        using var rsa = RSA.Create(Constants.RsaKeySize);

        // Export PEM
        string privatePem = rsa.ExportRSAPrivateKeyPem();
        string publicPem  = rsa.ExportRSAPublicKeyPem();

        // Write private key (restricted permissions)
        File.WriteAllText(privateKeyPath, privatePem, Encoding.ASCII);
        TryRestrictFilePermissions(privateKeyPath);

        // Write public key
        File.WriteAllText(publicKeyPath, publicPem, Encoding.ASCII);

        // Zero the in-memory PEM strings as best we can
        // (strings are immutable, but we overwrite the char buffer via unsafe)
        UnsafeClearString(privatePem);
    }

    // ── Key loading ─────────────────────────────────────────────────

    /// <summary>
    /// Loads an RSA public key from a PEM file.
    /// Supports both PKCS#1 (BEGIN RSA PUBLIC KEY) and X.509/SPKI (BEGIN PUBLIC KEY) formats.
    /// </summary>
    public static RSA LoadPublicKey(string path)
    {
        string pem = File.ReadAllText(path).Trim();
        var rsa = RSA.Create();
        try
        {
            if (pem.Contains("BEGIN RSA PUBLIC KEY"))
                rsa.ImportFromPem(pem);
            else if (pem.Contains("BEGIN PUBLIC KEY"))
                rsa.ImportFromPem(pem);
            else
                throw new CryptographicException("Unrecognized public key PEM format.");

            ValidateKeySize(rsa);
            return rsa;
        }
        catch
        {
            rsa.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Loads an RSA private key from a PEM file.
    /// Supports PKCS#1 and PKCS#8 formats.
    /// </summary>
    public static RSA LoadPrivateKey(string path)
    {
        string pem = File.ReadAllText(path).Trim();
        var rsa = RSA.Create();
        try
        {
            rsa.ImportFromPem(pem);
            ValidateKeySize(rsa);
            return rsa;
        }
        catch
        {
            rsa.Dispose();
            throw;
        }
        finally
        {
            UnsafeClearString(pem);
        }
    }

    /// <summary>
    /// Loads an RSA public key from a PEM string (for programmatic integration).
    /// </summary>
    public static RSA LoadPublicKeyFromPem(string pem)
    {
        var rsa = RSA.Create();
        try
        {
            rsa.ImportFromPem(pem);
            ValidateKeySize(rsa);
            return rsa;
        }
        catch
        {
            rsa.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Loads an RSA private key from a PEM string (for programmatic integration).
    /// </summary>
    public static RSA LoadPrivateKeyFromPem(string pem)
    {
        var rsa = RSA.Create();
        try
        {
            rsa.ImportFromPem(pem);
            ValidateKeySize(rsa);
            return rsa;
        }
        catch
        {
            rsa.Dispose();
            throw;
        }
    }

    // ── Key loading from stdin / environment ────────────────────────

    /// <summary>
    /// Reads a PEM key from standard input (for piping in scripts / CI).
    /// </summary>
    public static string ReadKeyFromStdin()
    {
        using var reader = new StreamReader(Console.OpenStandardInput(), Encoding.ASCII);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Reads a PEM key from an environment variable.
    /// </summary>
    public static string ReadKeyFromEnvironment(string variableName)
    {
        return Environment.GetEnvironmentVariable(variableName)
            ?? throw new InvalidOperationException(
                $"Environment variable '{variableName}' is not set.");
    }

    // ── Helpers ─────────────────────────────────────────────────────

    private static void ValidateKeySize(RSA rsa)
    {
        if (rsa.KeySize < Constants.RsaKeySize)
            throw new CryptographicException(
                $"RSA key size {rsa.KeySize} bits is below the minimum of {Constants.RsaKeySize} bits.");
    }

    /// <summary>
    /// Best-effort in-memory clearing of a .NET string.
    /// Strings are immutable and interned; this is defense-in-depth.
    /// </summary>
    private static unsafe void UnsafeClearString(string s)
    {
        if (string.IsNullOrEmpty(s)) return;
        fixed (char* ptr = s)
        {
            for (int i = 0; i < s.Length; i++)
                ptr[i] = '\0';
        }
    }

    /// <summary>
    /// Attempts to restrict file permissions to owner-only (Unix).
    /// Silently ignored on Windows.
    /// </summary>
    private static void TryRestrictFilePermissions(string path)
    {
        try
        {
            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(path,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }
        }
        catch
        {
            // Non-critical – best effort
        }
    }
}
