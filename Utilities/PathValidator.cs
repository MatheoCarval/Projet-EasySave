using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.RegularExpressions;

<<<<<<< HEAD
// ============================================================================
// REQUIRED NUGET PACKAGES (for .NET Core / .NET 5+ / .NET 8+)
// ============================================================================
// The following NuGet packages must be installed for this code to compile:
//
// 1. System.IO.FileSystem.AccessControl
//    - Provides: FileSecurity, DirectorySecurity, FileSystemAccessRule
//    - Install: dotnet add package System.IO.FileSystem.AccessControl
//
// 2. System.Security.Principal.Windows
//    - Provides: WindowsIdentity, WindowsPrincipal, SecurityIdentifier
//    - Install: dotnet add package System.Security.Principal.Windows
//
// Note: These are included by default in .NET Framework but must be added
// explicitly in .NET Core and later versions.
// ============================================================================

// ============================================================================
// PLATFORM COMPATIBILITY
// ============================================================================
// - Windows: Full functionality including ACL-based permission checking
// - Linux/Mac: Falls back to file operation testing (FallbackWriteAccessCheck)
// - The code handles PlatformNotSupportedException gracefully for cross-platform use
// ============================================================================

namespace FileSystemValidation
{
    /// <summary>
    /// Provides comprehensive path validation and verification functionality.
    /// This class validates paths for correctness, existence, and accessibility
    /// while ensuring security and platform compatibility.
    /// </summary>
    public class PathValidator
    {
        // Maximum path length for Windows (260) and common limitation
        private const int MaxPathLength = 260;
        
        // Invalid characters that cannot appear in file paths (platform-independent)
        private static readonly char[] InvalidPathChars = Path.GetInvalidPathChars();
        
        // Regex pattern to detect potentially dangerous path traversal attempts
        private static readonly Regex PathTraversalPattern = new Regex(@"\.\.[/\\]", RegexOptions.Compiled);

        /// <summary>
        /// Validates if a given path string is structurally valid and safe to use.
        /// Checks for null/empty values, invalid characters, path traversal attempts,
        /// excessive length, and proper format.
        /// </summary>
        /// <param name="path">The path string to validate</param>
        /// <returns>True if the path is valid and safe; otherwise false</returns>
        public bool IsValidPath(string path)
        {
            // Reject null or empty paths
=======

namespace FileSystemValidation
{
    public static class PathValidator
    {
        private const int MaxPathLength = 260;
        private static readonly char[] InvalidPathChars = Path.GetInvalidPathChars();
        private static readonly Regex PathTraversalPattern = new Regex(@"\.\.[/\\]", RegexOptions.Compiled);

        public static bool IsValidPath(string path)
        {
>>>>>>> develop
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            try
            {
<<<<<<< HEAD
                // Check path length against system limitations
=======
>>>>>>> develop
                if (path.Length > MaxPathLength)
                {
                    return false;
                }

<<<<<<< HEAD
                // Detect path traversal attempts (../ or ..\) for security
=======
>>>>>>> develop
                if (PathTraversalPattern.IsMatch(path))
                {
                    return false;
                }

<<<<<<< HEAD
                // Verify no invalid characters are present in the path
=======
>>>>>>> develop
                if (path.IndexOfAny(InvalidPathChars) >= 0)
                {
                    return false;
                }

<<<<<<< HEAD
                // Use Path.GetFullPath to validate format - throws on invalid paths
                string fullPath = Path.GetFullPath(path);
                
                // Additional check: ensure the path has a valid root
=======
                string fullPath = Path.GetFullPath(path);
                
>>>>>>> develop
                if (!Path.IsPathRooted(fullPath))
                {
                    return false;
                }

                return true;
            }
            catch (ArgumentException)
            {
<<<<<<< HEAD
                // Path contains invalid characters or format
=======
>>>>>>> develop
                return false;
            }
            catch (NotSupportedException)
            {
<<<<<<< HEAD
                // Path format is not supported
=======
>>>>>>> develop
                return false;
            }
            catch (PathTooLongException)
            {
<<<<<<< HEAD
                // Path exceeds maximum length
=======
>>>>>>> develop
                return false;
            }
            catch (Exception)
            {
<<<<<<< HEAD
                // Any other unexpected exception means invalid path
=======
>>>>>>> develop
                return false;
            }
        }

<<<<<<< HEAD
        /// <summary>
        /// Determines if a path represents a valid network path (UNC path).
        /// Network paths follow the format \\server\share\path
        /// </summary>
        /// <param name="path">The path to check</param>
        /// <returns>True if the path is a valid network path; otherwise false</returns>
        public bool IsNetworkPath(string path)
        {
            // First validate the basic path structure
=======
        public static bool IsNetworkPath(string path)
        {
>>>>>>> develop
            if (!IsValidPath(path))
            {
                return false;
            }

            try
            {
<<<<<<< HEAD
                // Get the full path to normalize it
                string fullPath = Path.GetFullPath(path);
                
                // Check if path starts with \\ (UNC path indicator)
                // and has at least server and share components
                if (fullPath.StartsWith(@"\\") || fullPath.StartsWith(@"//"))
                {
                    // Split the path and verify it has minimum required components
                    // Format: \\server\share (minimum valid UNC path)
                    string[] parts = fullPath.TrimStart('\\', '/').Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
                    
                    // Must have at least server and share name
=======
                string fullPath = Path.GetFullPath(path);
                
                if (fullPath.StartsWith(@"\\") || fullPath.StartsWith(@"//"))
                {
                    string[] parts = fullPath.TrimStart('\\', '/').Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
                    
>>>>>>> develop
                    return parts.Length >= 2;
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

<<<<<<< HEAD
        /// <summary>
        /// Converts a local path to its Universal Naming Convention (UNC) format.
        /// UNC paths use the format \\server\share\path for network resources.
        /// Local paths are converted to administrative shares format (e.g., C:\folder becomes \\localhost\C$\folder).
        /// </summary>
        /// <param name="path">The path to convert</param>
        /// <returns>The UNC representation of the path, or empty string if invalid</returns>
        public string ToUncPath(string path)
        {
            // Validate the path before conversion
=======
        public static string ToUncPath(string path)
        {
>>>>>>> develop
            if (!IsValidPath(path))
            {
                return string.Empty;
            }

            try
            {
<<<<<<< HEAD
                // Get absolute path to normalize it
                string fullPath = Path.GetFullPath(path);
                
                // If already a UNC path, return as-is
=======
                string fullPath = Path.GetFullPath(path);
                
>>>>>>> develop
                if (IsNetworkPath(fullPath))
                {
                    return fullPath;
                }
<<<<<<< HEAD

                // Convert local path to UNC format using administrative shares
                // Example: C:\folder\file.txt becomes \\localhost\C$\folder\file.txt
                
                // Extract drive letter and remaining path
=======
                
>>>>>>> develop
                string? driveLetterNullable = Path.GetPathRoot(fullPath);
                if (string.IsNullOrEmpty(driveLetterNullable))
                {
                    return string.Empty;
                }
                string driveLetter = driveLetterNullable.TrimEnd('\\', ':');

<<<<<<< HEAD
                // Get the path without the root
=======
>>>>>>> develop
                string? root = Path.GetPathRoot(fullPath);
                if (string.IsNullOrEmpty(root))
                {
                    return string.Empty;
                }
                string pathWithoutRoot = fullPath.Substring(root.Length);
                
<<<<<<< HEAD
                // Build UNC path: \\localhost\C$\path\to\file
=======
>>>>>>> develop
                string uncPath = $@"\\localhost\{driveLetter}$\{pathWithoutRoot}";
                
                return uncPath;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

<<<<<<< HEAD
        /// <summary>
        /// Checks if a file or directory exists at the specified path.
        /// Works for both files and directories.
        /// </summary>
        /// <param name="path">The path to check for existence</param>
        /// <returns>True if a file or directory exists at the path; otherwise false</returns>
        public bool PathExists(string path)
        {
            // Validate path structure first
=======
        public static bool PathExists(string path)
        {
>>>>>>> develop
            if (!IsValidPath(path))
            {
                return false;
            }

            try
            {
<<<<<<< HEAD
                // Check both file and directory existence
                // Using File.Exists and Directory.Exists is more efficient than FileSystemInfo
=======
>>>>>>> develop
                return File.Exists(path) || Directory.Exists(path);
            }
            catch (Exception)
            {
<<<<<<< HEAD
                // Any exception (access denied, etc.) means we can't confirm existence
=======
>>>>>>> develop
                return false;
            }
        }

<<<<<<< HEAD
        /// <summary>
        /// Determines if the specified path represents a directory.
        /// Returns false if path doesn't exist or is a file.
        /// </summary>
        /// <param name="path">The path to check</param>
        /// <returns>True if the path exists and is a directory; otherwise false</returns>
        public bool IsDirectory(string path)
        {
            // Validate path structure first
=======
        public static bool IsDirectory(string path)
        {
>>>>>>> develop
            if (!IsValidPath(path))
            {
                return false;
            }

            try
            {
<<<<<<< HEAD
                // Use FileAttributes to determine if it's a directory
                // This is more reliable than just checking Directory.Exists
=======
>>>>>>> develop
                FileAttributes attributes = File.GetAttributes(path);
                return (attributes & FileAttributes.Directory) == FileAttributes.Directory;
            }
            catch (FileNotFoundException)
            {
<<<<<<< HEAD
                // Path doesn't exist
=======
>>>>>>> develop
                return false;
            }
            catch (DirectoryNotFoundException)
            {
<<<<<<< HEAD
                // Directory doesn't exist
=======
>>>>>>> develop
                return false;
            }
            catch (Exception)
            {
<<<<<<< HEAD
                // Access denied or other error
=======
>>>>>>> develop
                return false;
            }
        }

<<<<<<< HEAD
        /// <summary>
        /// Verifies if the current user has write access to the specified path.
        /// Checks actual ACL permissions for both files and directories.
        /// Note: This checks permissions, not current availability (file may be locked by another process).
        /// </summary>
        /// <param name="path">The path to check for write access</param>
        /// <returns>True if write permission is granted; otherwise false</returns>
        public bool HasWriteAccess(string path)
        {
            // Validate path first
=======
        public static bool HasWriteAccess(string path)
        {
>>>>>>> develop
            if (!IsValidPath(path))
            {
                return false;
            }

<<<<<<< HEAD
            // Only use ACL checking on Windows
=======
>>>>>>> develop
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return FallbackWriteAccessCheck(path);
            }

            try
            {
<<<<<<< HEAD
                // Get the current user's identity
=======
>>>>>>> develop
                WindowsIdentity currentUser = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(currentUser);

                string targetPath = path;

<<<<<<< HEAD
                // If path doesn't exist, check parent directory's write access
=======
>>>>>>> develop
                if (!PathExists(path))
                {
                    string? directoryPath = Path.GetDirectoryName(path);
                    if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
                    {
                        return false;
                    }
                    targetPath = directoryPath!;
                }

<<<<<<< HEAD
                // Determine if we're checking a directory or file
                bool isDirectory = IsDirectory(targetPath);

                // Get the appropriate ACL (Access Control List)
=======
                bool isDirectory = IsDirectory(targetPath);

>>>>>>> develop
                AuthorizationRuleCollection rules;
                
                if (isDirectory)
                {
<<<<<<< HEAD
                    // Use static method from FileSystemAclExtensions
=======
>>>>>>> develop
                    DirectoryInfo dirInfo = new DirectoryInfo(targetPath);
                    DirectorySecurity dirSecurity = FileSystemAclExtensions.GetAccessControl(dirInfo);
                    rules = dirSecurity.GetAccessRules(true, true, typeof(SecurityIdentifier));
                }
                else
                {
<<<<<<< HEAD
                    // Use static method from FileSystemAclExtensions
=======
>>>>>>> develop
                    FileInfo fileInfo = new FileInfo(targetPath);
                    FileSecurity fileSecurity = FileSystemAclExtensions.GetAccessControl(fileInfo);
                    rules = fileSecurity.GetAccessRules(true, true, typeof(SecurityIdentifier));
                }

<<<<<<< HEAD
                // Check if any rule explicitly allows write access
=======
>>>>>>> develop
                bool hasWritePermission = false;
                bool isDenied = false;

                foreach (FileSystemAccessRule rule in rules)
                {
<<<<<<< HEAD
                    // Check if this rule applies to the current user
                    if ((currentUser.User != null && currentUser.User.Equals(rule.IdentityReference)) ||
                        principal.IsInRole((SecurityIdentifier)rule.IdentityReference))
                    {
                        // Check for Write, Modify, or FullControl permissions
=======
                    if ((currentUser.User != null && currentUser.User.Equals(rule.IdentityReference)) ||
                        principal.IsInRole((SecurityIdentifier)rule.IdentityReference))
                    {
>>>>>>> develop
                        if ((rule.FileSystemRights & FileSystemRights.Write) == FileSystemRights.Write ||
                            (rule.FileSystemRights & FileSystemRights.Modify) == FileSystemRights.Modify ||
                            (rule.FileSystemRights & FileSystemRights.FullControl) == FileSystemRights.FullControl)
                        {
                            if (rule.AccessControlType == AccessControlType.Deny)
                            {
<<<<<<< HEAD
                                // Deny rules take precedence
=======
>>>>>>> develop
                                isDenied = true;
                                break;
                            }
                            else if (rule.AccessControlType == AccessControlType.Allow)
                            {
                                hasWritePermission = true;
                            }
                        }
                    }
                }

<<<<<<< HEAD
                // If explicitly denied, return false regardless of allow rules
=======
>>>>>>> develop
                if (isDenied)
                {
                    return false;
                }

                return hasWritePermission;
            }
            catch (UnauthorizedAccessException)
            {
<<<<<<< HEAD
                // Cannot read ACL - likely no access
=======
>>>>>>> develop
                return false;
            }
            catch (PlatformNotSupportedException)
            {
<<<<<<< HEAD
                // ACL checking not supported on this platform (e.g., Linux)
                // Fall back to simple write test
=======
>>>>>>> develop
                return FallbackWriteAccessCheck(path);
            }
            catch (Exception)
            {
<<<<<<< HEAD
                // Any other exception - try fallback method
=======
>>>>>>> develop
                return FallbackWriteAccessCheck(path);
            }
        }

<<<<<<< HEAD
        /// <summary>
        /// Fallback method for checking write access when ACL checking is unavailable.
        /// Used on non-Windows platforms or when ACL checks fail.
        /// Note: This tests actual write availability, not just permissions.
        /// </summary>
        /// <param name="path">The path to check</param>
        /// <returns>True if can write now; otherwise false</returns>
        private bool FallbackWriteAccessCheck(string path)
=======
        private static bool FallbackWriteAccessCheck(string path)
>>>>>>> develop
        {
            try
            {
                string targetPath = path;

                if (!PathExists(path))
                {
                    string? directoryPath = Path.GetDirectoryName(path);
                    if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
                    {
                        return false;
                    }
                    targetPath = directoryPath;
                }

                bool isDirectory = IsDirectory(targetPath);

                if (isDirectory)
                {
<<<<<<< HEAD
                    // Test directory write access
=======
>>>>>>> develop
                    string testFile = Path.Combine(targetPath, $".write_test_{Guid.NewGuid()}.tmp");
                    
                    try
                    {
                        using (FileStream fs = File.Create(testFile, 1, FileOptions.DeleteOnClose))
                        {
<<<<<<< HEAD
                            // Successfully created
=======
>>>>>>> develop
                        }
                        return true;
                    }
                    catch
                    {
                        if (File.Exists(testFile))
                        {
                            try { File.Delete(testFile); } catch { }
                        }
                        return false;
                    }
                }
                else
                {
<<<<<<< HEAD
                    // For files, attempt to open with write access
=======
>>>>>>> develop
                    using (FileStream fs = File.Open(targetPath, FileMode.Open, FileAccess.Write))
                    {
                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }
        }
    }
}