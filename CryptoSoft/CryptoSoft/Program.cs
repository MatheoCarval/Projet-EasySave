using System.CommandLine;
using CryptoSoft.CLI;

// ═══════════════════════════════════════════════════════════════════
//  CryptoSoft – Hybrid asymmetric encryption CLI
//  RSA-OAEP-SHA256 + AES-256-GCM envelope encryption
// ═══════════════════════════════════════════════════════════════════

using var mutex = new Mutex(true, @"Global\CryptoSoft_SingleInstance", out bool createdNew);
if (!createdNew)
{
    Console.Error.WriteLine("CryptoSoft is already running. Only one instance is allowed at a time.");
    return 1;
}

var rootCommand = new RootCommand(
    "CryptoSoft – Hybrid asymmetric encryption for secure backups.\n" +
    "Uses RSA-4096 (OAEP-SHA256) to wrap per-file AES-256-GCM session keys.\n" +
    "No passwords. Public key encrypts; private key decrypts.");

rootCommand.AddCommand(CliCommands.BuildKeygenCommand());
rootCommand.AddCommand(CliCommands.BuildEncryptCommand());
rootCommand.AddCommand(CliCommands.BuildDecryptCommand());
rootCommand.AddCommand(CliCommands.BuildInfoCommand());

return await rootCommand.InvokeAsync(args);
