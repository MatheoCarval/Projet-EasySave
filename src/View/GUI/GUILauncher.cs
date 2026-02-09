using Avalonia;
using System;
using EasySave.Services;
using EasySave.Services.Managers;
using Services.Managers;

namespace EasySave.View.GUI;

/// <summary>
/// Helper class to launch the Avalonia GUI
/// </summary>
public static class GUILauncher
{
    /// <summary>
    /// Starts the Avalonia application with required services
    /// </summary>
    public static void Launch(LocalizationService localizationService, BackupManager backupManager)
    {
        App.LocalizationService = localizationService;
        App.BackupManager = backupManager;

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(Array.Empty<string>());
    }

    /// <summary>
    /// Configures and builds the Avalonia application
    /// </summary>
    /// <returns>AppBuilder instance</returns>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
