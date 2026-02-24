using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace EasySave.Services;

/// <summary>
/// Provides native Windows 10/11 toast notification support.
/// Uses PowerShell to invoke WinRT toast APIs — no additional NuGet packages required.
/// </summary>
public static class NotificationService
{
    /// <summary>
    /// Shows a Windows toast notification with the given title and message.
    /// Silently does nothing on non-Windows platforms.
    /// </summary>
    public static void Show(string title, string message)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

        try
        {
            var safeTitle = EscapeForPowerShell(title);
            var safeMessage = EscapeForPowerShell(message);

            var script = $@"
[Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] > $null
$template = [Windows.UI.Notifications.ToastNotificationManager]::GetTemplateContent([Windows.UI.Notifications.ToastTemplateType]::ToastText02)
$textNodes = $template.GetElementsByTagName('text')
$textNodes[0].AppendChild($template.CreateTextNode('{safeTitle}')) > $null
$textNodes[1].AppendChild($template.CreateTextNode('{safeMessage}')) > $null
$toast = [Windows.UI.Notifications.ToastNotification]::new($template)
[Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier('EasySave').Show($toast)
";
            // Base64-encode the script to avoid all shell escaping issues
            var bytes = Encoding.Unicode.GetBytes(script);
            var encoded = Convert.ToBase64String(bytes);

            Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -NonInteractive -EncodedCommand {encoded}",
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            });
        }
        catch
        {
            // Silently ignore notification failures — never crash the app for a notification
        }
    }

    /// <summary>
    /// Shows a success notification for a completed backup.
    /// </summary>
    public static void NotifyBackupCompleted(string jobName)
    {
        Show("EasySave ✓", $"{jobName}");
    }

    /// <summary>
    /// Shows an error notification for a failed backup.
    /// </summary>
    public static void NotifyBackupFailed(string jobName, string reason)
    {
        Show("EasySave ✗", $"{jobName} — {reason}");
    }

    private static string EscapeForPowerShell(string input)
    {
        return input.Replace("'", "''");
    }
}
