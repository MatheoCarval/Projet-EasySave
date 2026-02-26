using System.CommandLine;
using System.Diagnostics;
using CryptoSoft.Services;
using CryptoSoft.Models;
using CryptoSoft.Core;

namespace CryptoSoft.CLI;

/// <summary>
/// Defines the CLI commands: keygen, encrypt, decrypt.
/// All commands are non-interactive and support scripting / pipeline integration.
/// </summary>
public static class CliCommands
{
    // ═══════════════════════════════════════════════════════════════
    //  keygen
    // ═══════════════════════════════════════════════════════════════

    public static Command BuildKeygenCommand()
    {
        var privateOpt = new Option<FileInfo>(
            aliases: new[] { "--private", "-s" },
            description: "Output path for the RSA-4096 private key (PEM)")
        { IsRequired = true };

        var publicOpt = new Option<FileInfo>(
            aliases: new[] { "--public", "-p" },
            description: "Output path for the RSA-4096 public key (PEM)")
        { IsRequired = true };

        var cmd = new Command("keygen", "Generate an RSA-4096 key pair (PEM files)")
        {
            privateOpt,
            publicOpt
        };

        cmd.SetHandler((FileInfo privFile, FileInfo pubFile) =>
        {
            var sw = Stopwatch.StartNew();
            Console.Error.Write("Generating RSA-4096 key pair... ");

            KeyManagement.GenerateKeyPair(privFile.FullName, pubFile.FullName);

            sw.Stop();
            Console.Error.WriteLine($"done ({sw.ElapsedMilliseconds} ms).");
            Console.Error.WriteLine($"  Private key: {privFile.FullName}");
            Console.Error.WriteLine($"  Public key:  {pubFile.FullName}");
        }, privateOpt, publicOpt);

        return cmd;
    }

    // ═══════════════════════════════════════════════════════════════
    //  encrypt
    // ═══════════════════════════════════════════════════════════════

    public static Command BuildEncryptCommand()
    {
        var inputOpt = new Option<string?>(
            aliases: new[] { "--input", "-i" },
            description: "Input file or directory (omit for stdin)");

        var outputOpt = new Option<string?>(
            aliases: new[] { "--output", "-o" },
            description: "Output encrypted file (omit for stdout)");

        var pubkeyOpt = new Option<string>(
            aliases: new[] { "--pubkey", "-k" },
            description: "Path to RSA public key (PEM)")
        { IsRequired = true };

        var compressOpt = new Option<bool>(
            aliases: new[] { "--compress", "-c" },
            description: "Compress directory before encryption (directories are always compressed)")
        { };

        var cmd = new Command("encrypt", "Encrypt a file, directory, or stdin with a public key")
        {
            inputOpt,
            outputOpt,
            pubkeyOpt,
            compressOpt
        };

        cmd.SetHandler((string? input, string? output, string pubkey, bool compress) =>
        {
            using var rsa = KeyManagement.LoadPublicKey(pubkey);
            var sw = Stopwatch.StartNew();

            if (input == null && output == null)
            {
                // stdin → stdout
                FileStreamProcessor.EncryptStdio(rsa);
            }
            else if (input != null && Directory.Exists(input))
            {
                // Directory mode
                string outPath = output ?? (input.TrimEnd(Path.DirectorySeparatorChar) + ".enc");
                Console.Error.Write($"Encrypting directory '{input}'... ");
                FileStreamProcessor.EncryptDirectory(input, outPath, rsa);
                sw.Stop();
                PrintEncryptionStats(sw, new FileInfo(outPath).Length);
            }
            else if (input != null)
            {
                // Single file
                string outPath = output ?? (input + ".enc");
                Console.Error.Write($"Encrypting '{input}'... ");
                FileStreamProcessor.EncryptFile(input, outPath, rsa);
                sw.Stop();
                PrintEncryptionStats(sw, new FileInfo(outPath).Length);
            }
            else
            {
                // stdin → file
                string outPath = output!;
                Console.Error.Write("Encrypting from stdin... ");
                using var outStream = new FileStream(outPath, FileMode.Create, FileAccess.Write);
                using var inStream = Console.OpenStandardInput();
                CryptoEngine.Encrypt(inStream, outStream, rsa);
                sw.Stop();
                PrintEncryptionStats(sw, new FileInfo(outPath).Length);
            }
        }, inputOpt, outputOpt, pubkeyOpt, compressOpt);

        return cmd;
    }

    // ═══════════════════════════════════════════════════════════════
    //  decrypt
    // ═══════════════════════════════════════════════════════════════

    public static Command BuildDecryptCommand()
    {
        var inputOpt = new Option<string?>(
            aliases: new[] { "--input", "-i" },
            description: "Input encrypted file (omit for stdin)");

        var outputOpt = new Option<string?>(
            aliases: new[] { "--output", "-o" },
            description: "Output decrypted file or directory (omit for stdout)");

        var privkeyOpt = new Option<string>(
            aliases: new[] { "--privkey", "-k" },
            description: "Path to RSA private key (PEM)")
        { IsRequired = true };

        var directoryOpt = new Option<bool>(
            aliases: new[] { "--directory", "-d" },
            description: "Decrypt and decompress a directory archive");

        var cmd = new Command("decrypt", "Decrypt a file, directory archive, or stdin with a private key")
        {
            inputOpt,
            outputOpt,
            privkeyOpt,
            directoryOpt
        };

        cmd.SetHandler((string? input, string? output, string privkey, bool directory) =>
        {
            using var rsa = KeyManagement.LoadPrivateKey(privkey);
            var sw = Stopwatch.StartNew();

            if (input == null && output == null)
            {
                // stdin → stdout
                FileStreamProcessor.DecryptStdio(rsa);
            }
            else if (directory && input != null && output != null)
            {
                // Directory mode
                Console.Error.Write($"Decrypting directory archive '{input}'... ");
                FileStreamProcessor.DecryptDirectory(input, output, rsa);
                sw.Stop();
                Console.Error.WriteLine($"done ({sw.Elapsed.TotalSeconds:F2}s).");
            }
            else if (input != null)
            {
                // Single file
                string outPath = output ?? Path.GetFileNameWithoutExtension(input);
                Console.Error.Write($"Decrypting '{input}'... ");
                FileStreamProcessor.DecryptFile(input, outPath, rsa);
                sw.Stop();
                long outSize = new FileInfo(outPath).Length;
                Console.Error.WriteLine(
                    $"done ({sw.Elapsed.TotalSeconds:F2}s, {FormatSize(outSize)} written).");
            }
            else
            {
                // stdin → file
                string outPath = output!;
                Console.Error.Write("Decrypting from stdin... ");
                using var inStream = Console.OpenStandardInput();
                using var outStream = new FileStream(outPath, FileMode.Create, FileAccess.Write);
                CryptoEngine.Decrypt(inStream, outStream, rsa);
                sw.Stop();
                Console.Error.WriteLine($"done ({sw.Elapsed.TotalSeconds:F2}s).");
            }
        }, inputOpt, outputOpt, privkeyOpt, directoryOpt);

        return cmd;
    }

    // ═══════════════════════════════════════════════════════════════
    //  info (inspect encrypted file header)
    // ═══════════════════════════════════════════════════════════════

    public static Command BuildInfoCommand()
    {
        var inputOpt = new Option<string>(
            aliases: new[] { "--input", "-i" },
            description: "Path to an encrypted CryptoSoft file")
        { IsRequired = true };

        var cmd = new Command("info", "Display encrypted file header information")
        {
            inputOpt
        };

        cmd.SetHandler((string input) =>
        {
            using var fs = File.OpenRead(input);
            var header = CryptoFileHeader.ReadFrom(fs);

            Console.WriteLine($"CryptoSoft Encrypted File");
            Console.WriteLine($"  Format version : {header.Version}");
            Console.WriteLine($"  Algorithm      : {AlgorithmName(header.Algorithm)}");
            Console.WriteLine($"  Nonce          : {Convert.ToHexString(header.Nonce)}");
            Console.WriteLine($"  Wrapped key    : {header.WrappedKey.Length} bytes");
            Console.WriteLine($"  Header size    : {header.SerializedSize} bytes");
            Console.WriteLine($"  File size      : {FormatSize(new FileInfo(input).Length)}");
        }, inputOpt);

        return cmd;
    }

    // ─── Helpers ────────────────────────────────────────────────────

    private static void PrintEncryptionStats(Stopwatch sw, long outputSize)
    {
        Console.Error.WriteLine(
            $"done ({sw.Elapsed.TotalSeconds:F2}s, {FormatSize(outputSize)} output).");
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024          => $"{bytes} B",
        < 1024 * 1024   => $"{bytes / 1024.0:F1} KiB",
        < 1024L * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MiB",
        _               => $"{bytes / (1024.0 * 1024 * 1024):F2} GiB"
    };

    private static string AlgorithmName(byte id) => id switch
    {
        Constants.AlgorithmRsaOaepAes256Gcm => "RSA-OAEP-SHA256 + AES-256-GCM (hybrid)",
        _ => $"Unknown (0x{id:X2})"
    };
}
