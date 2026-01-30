using System;
using System.IO;
using FileSystemValidation;

namespace PathValidatorDemo
{
    /// <summary>
    /// Main program to demonstrate PathValidator functionality.
    /// Tests all methods with various path scenarios.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("╔═══════════════════════════════════════════════╗");
            Console.WriteLine("║      PathValidator Demonstration Program      ║");
            Console.WriteLine("╚═══════════════════════════════════════════════╝");
            Console.WriteLine();

            // Create an instance of PathValidator
            PathValidator validator = new PathValidator();

            // Test paths - modify these according to your system
            string[] testPaths = new string[]
            {
                @"C:\Windows\System32",              // Valid directory
                @"C:\Program Files",                 // Valid directory (may need admin)
                @"C:\temp\test.txt",                 // File that may or may not exist
                @"\\localhost\C$\Windows",           // UNC path
                @"C:\Users\..\Windows",              // Path with traversal
                @"",                                 // Empty path
                null,                                // Null path
                @"C:\Invalid<>Path",                 // Invalid characters
                @"\\server\share\folder",            // Network path
                Directory.GetCurrentDirectory()      // Current directory
            };

            // Run all tests
            Console.WriteLine("═══════════════════════════════════════════════");
            Console.WriteLine("Testing IsValidPath()");
            Console.WriteLine("═══════════════════════════════════════════════");
            TestIsValidPath(validator, testPaths);

            Console.WriteLine("\n═══════════════════════════════════════════════");
            Console.WriteLine("Testing IsNetworkPath()");
            Console.WriteLine("═══════════════════════════════════════════════");
            TestIsNetworkPath(validator, testPaths);

            Console.WriteLine("\n═══════════════════════════════════════════════");
            Console.WriteLine("Testing ToUncPath()");
            Console.WriteLine("═══════════════════════════════════════════════");
            TestToUncPath(validator, testPaths);

            Console.WriteLine("\n═══════════════════════════════════════════════");
            Console.WriteLine("Testing PathExists()");
            Console.WriteLine("═══════════════════════════════════════════════");
            TestPathExists(validator, testPaths);

            Console.WriteLine("\n═══════════════════════════════════════════════");
            Console.WriteLine("Testing IsDirectory()");
            Console.WriteLine("═══════════════════════════════════════════════");
            TestIsDirectory(validator, testPaths);

            Console.WriteLine("\n═══════════════════════════════════════════════");
            Console.WriteLine("Testing HasWriteAccess()");
            Console.WriteLine("═══════════════════════════════════════════════");
            TestHasWriteAccess(validator, testPaths);

            // Interactive mode
            Console.WriteLine("\n\n╔═══════════════════════════════════════════════╗");
            Console.WriteLine("║            Interactive Mode                    ║");
            Console.WriteLine("╚═══════════════════════════════════════════════╝");
            InteractiveMode(validator);
        }

        /// <summary>
        /// Tests the IsValidPath method with various paths.
        /// </summary>
        static void TestIsValidPath(PathValidator validator, string[] paths)
        {
            foreach (string path in paths)
            {
                string displayPath = path ?? "<null>";
                bool isValid = validator.IsValidPath(path);
                
                Console.ForegroundColor = isValid ? ConsoleColor.Green : ConsoleColor.Red;
                Console.Write($"  [{(isValid ? "✓" : "✗")}] ");
                Console.ResetColor();
                Console.WriteLine($"{displayPath,-40} => {isValid}");
            }
        }

        /// <summary>
        /// Tests the IsNetworkPath method with various paths.
        /// </summary>
        static void TestIsNetworkPath(PathValidator validator, string[] paths)
        {
            foreach (string path in paths)
            {
                if (string.IsNullOrEmpty(path)) continue;
                
                bool isNetwork = validator.IsNetworkPath(path);
                
                Console.ForegroundColor = isNetwork ? ConsoleColor.Cyan : ConsoleColor.Gray;
                Console.Write($"  [{(isNetwork ? "NET" : "LOC")}] ");
                Console.ResetColor();
                Console.WriteLine($"{path,-40} => {isNetwork}");
            }
        }

        /// <summary>
        /// Tests the ToUncPath method with various paths.
        /// </summary>
        static void TestToUncPath(PathValidator validator, string[] paths)
        {
            foreach (string path in paths)
            {
                if (string.IsNullOrEmpty(path)) continue;
                
                string uncPath = validator.ToUncPath(path);
                
                if (!string.IsNullOrEmpty(uncPath))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("  [UNC] ");
                    Console.ResetColor();
                    Console.WriteLine($"{path,-35} => {uncPath}");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"  [---] {path,-35} => <conversion failed>");
                    Console.ResetColor();
                }
            }
        }

        /// <summary>
        /// Tests the PathExists method with various paths.
        /// </summary>
        static void TestPathExists(PathValidator validator, string[] paths)
        {
            foreach (string path in paths)
            {
                if (string.IsNullOrEmpty(path)) continue;
                
                bool exists = validator.PathExists(path);
                
                Console.ForegroundColor = exists ? ConsoleColor.Green : ConsoleColor.DarkYellow;
                Console.Write($"  [{(exists ? "EXISTS" : "MISSING")}] ");
                Console.ResetColor();
                Console.WriteLine($"{path}");
            }
        }

        /// <summary>
        /// Tests the IsDirectory method with various paths.
        /// </summary>
        static void TestIsDirectory(PathValidator validator, string[] paths)
        {
            foreach (string path in paths)
            {
                if (string.IsNullOrEmpty(path)) continue;
                
                bool isDir = validator.IsDirectory(path);
                
                if (validator.PathExists(path))
                {
                    Console.ForegroundColor = isDir ? ConsoleColor.Blue : ConsoleColor.Magenta;
                    Console.Write($"  [{(isDir ? "DIR" : "FILE")}] ");
                    Console.ResetColor();
                    Console.WriteLine($"{path}");
                }
            }
        }

        /// <summary>
        /// Tests the HasWriteAccess method with various paths.
        /// </summary>
        static void TestHasWriteAccess(PathValidator validator, string[] paths)
        {
            foreach (string path in paths)
            {
                if (string.IsNullOrEmpty(path)) continue;
                
                bool hasWrite = validator.HasWriteAccess(path);
                
                Console.ForegroundColor = hasWrite ? ConsoleColor.Green : ConsoleColor.Red;
                Console.Write($"  [{(hasWrite ? "WRITABLE" : "READ-ONLY")}] ");
                Console.ResetColor();
                Console.WriteLine($"{path}");
            }
        }

        /// <summary>
        /// Interactive mode allowing users to test custom paths.
        /// </summary>
        static void InteractiveMode(PathValidator validator)
        {
            Console.WriteLine("\nEnter a path to test (or 'exit' to quit):");
            
            while (true)
            {
                Console.Write("\n> ");
                string input = Console.ReadLine();
                
                if (string.IsNullOrWhiteSpace(input) || input.ToLower() == "exit")
                {
                    break;
                }

                Console.WriteLine();
                Console.WriteLine($"Path: {input}");
                Console.WriteLine(new string('─', 50));
                
                // Run all validations on the user's path
                bool isValid = validator.IsValidPath(input);
                Console.WriteLine($"  IsValidPath:      {GetStatusIcon(isValid)} {isValid}");
                
                if (isValid)
                {
                    bool isNetwork = validator.IsNetworkPath(input);
                    Console.WriteLine($"  IsNetworkPath:    {GetStatusIcon(isNetwork)} {isNetwork}");
                    
                    string uncPath = validator.ToUncPath(input);
                    Console.WriteLine($"  ToUncPath:        {(string.IsNullOrEmpty(uncPath) ? "❌" : "✓")} {uncPath}");
                    
                    bool exists = validator.PathExists(input);
                    Console.WriteLine($"  PathExists:       {GetStatusIcon(exists)} {exists}");
                    
                    if (exists)
                    {
                        bool isDir = validator.IsDirectory(input);
                        Console.WriteLine($"  IsDirectory:      {GetStatusIcon(isDir)} {isDir} ({(isDir ? "Directory" : "File")})");
                        
                        bool hasWrite = validator.HasWriteAccess(input);
                        Console.WriteLine($"  HasWriteAccess:   {GetStatusIcon(hasWrite)} {hasWrite}");
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("  ⚠ Invalid path - other tests skipped");
                    Console.ResetColor();
                }
            }
            
            Console.WriteLine("\nGoodbye! 👋");
        }

        /// <summary>
        /// Returns a visual icon for status display.
        /// </summary>
        static string GetStatusIcon(bool status)
        {
            return status ? "✓" : "✗";
        }
    }
}