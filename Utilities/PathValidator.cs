using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.RegularExpressions;


namespace FileSystemValidation
{
    public static class PathValidator
    {
        private const int MaxPathLength = 260;
        private static readonly char[] InvalidPathChars = Path.GetInvalidPathChars();
        private static readonly Regex PathTraversalPattern = new Regex(@"\.\.[/\\]", RegexOptions.Compiled);

        public static bool IsValidPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            try
            {
                if (path.Length > MaxPathLength)
                {
                    return false;
                }

                if (PathTraversalPattern.IsMatch(path))
                {
                    return false;
                }

                if (path.IndexOfAny(InvalidPathChars) >= 0)
                {
                    return false;
                }

                string fullPath = Path.GetFullPath(path);
                
                if (!Path.IsPathRooted(fullPath))
                {
                    return false;
                }

                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (NotSupportedException)
            {
                return false;
            }
            catch (PathTooLongException)
            {
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool IsNetworkPath(string path)
        {
            if (!IsValidPath(path))
            {
                return false;
            }

            try
            {
                string fullPath = Path.GetFullPath(path);
                
                if (fullPath.StartsWith(@"\\") || fullPath.StartsWith(@"//"))
                {
                    string[] parts = fullPath.TrimStart('\\', '/').Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
                    
                    return parts.Length >= 2;
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static string ToUncPath(string path)
        {
            if (!IsValidPath(path))
            {
                return string.Empty;
            }

            try
            {
                string fullPath = Path.GetFullPath(path);
                
                if (IsNetworkPath(fullPath))
                {
                    return fullPath;
                }
                
                string? driveLetterNullable = Path.GetPathRoot(fullPath);
                if (string.IsNullOrEmpty(driveLetterNullable))
                {
                    return string.Empty;
                }
                string driveLetter = driveLetterNullable.TrimEnd('\\', ':');

                string? root = Path.GetPathRoot(fullPath);
                if (string.IsNullOrEmpty(root))
                {
                    return string.Empty;
                }
                string pathWithoutRoot = fullPath.Substring(root.Length);
                
                string uncPath = $@"\\localhost\{driveLetter}$\{pathWithoutRoot}";
                
                return uncPath;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        public static bool PathExists(string path)
        {
            if (!IsValidPath(path))
            {
                return false;
            }

            try
            {
                return File.Exists(path) || Directory.Exists(path);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool IsDirectory(string path)
        {
            if (!IsValidPath(path))
            {
                return false;
            }

            try
            {
                FileAttributes attributes = File.GetAttributes(path);
                return (attributes & FileAttributes.Directory) == FileAttributes.Directory;
            }
            catch (FileNotFoundException)
            {
                return false;
            }
            catch (DirectoryNotFoundException)
            {
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool HasWriteAccess(string path)
        {
            if (!IsValidPath(path))
            {
                return false;
            }

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return FallbackWriteAccessCheck(path);
            }

            try
            {
                WindowsIdentity currentUser = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(currentUser);

                string targetPath = path;

                if (!PathExists(path))
                {
                    string? directoryPath = Path.GetDirectoryName(path);
                    if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
                    {
                        return false;
                    }
                    targetPath = directoryPath!;
                }

                bool isDirectory = IsDirectory(targetPath);

                AuthorizationRuleCollection rules;
                
                if (isDirectory)
                {
                    DirectoryInfo dirInfo = new DirectoryInfo(targetPath);
                    DirectorySecurity dirSecurity = FileSystemAclExtensions.GetAccessControl(dirInfo);
                    rules = dirSecurity.GetAccessRules(true, true, typeof(SecurityIdentifier));
                }
                else
                {
                    FileInfo fileInfo = new FileInfo(targetPath);
                    FileSecurity fileSecurity = FileSystemAclExtensions.GetAccessControl(fileInfo);
                    rules = fileSecurity.GetAccessRules(true, true, typeof(SecurityIdentifier));
                }

                bool hasWritePermission = false;
                bool isDenied = false;

                foreach (FileSystemAccessRule rule in rules)
                {
                    if ((currentUser.User != null && currentUser.User.Equals(rule.IdentityReference)) ||
                        principal.IsInRole((SecurityIdentifier)rule.IdentityReference))
                    {
                        if ((rule.FileSystemRights & FileSystemRights.Write) == FileSystemRights.Write ||
                            (rule.FileSystemRights & FileSystemRights.Modify) == FileSystemRights.Modify ||
                            (rule.FileSystemRights & FileSystemRights.FullControl) == FileSystemRights.FullControl)
                        {
                            if (rule.AccessControlType == AccessControlType.Deny)
                            {
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

                if (isDenied)
                {
                    return false;
                }

                return hasWritePermission;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (PlatformNotSupportedException)
            {
                return FallbackWriteAccessCheck(path);
            }
            catch (Exception)
            {
                return FallbackWriteAccessCheck(path);
            }
        }

        private static bool FallbackWriteAccessCheck(string path)
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
                    string testFile = Path.Combine(targetPath, $".write_test_{Guid.NewGuid()}.tmp");
                    
                    try
                    {
                        using (FileStream fs = File.Create(testFile, 1, FileOptions.DeleteOnClose))
                        {
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