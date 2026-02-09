using Avalonia;
using System;

namespace EasySave.View.GUI;

/// <summary>
/// Helper class to launch the Avalonia GUI
/// </summary>
public static class GUILauncher
{
    /// <summary>
    /// Starts the Avalonia application
    /// </summary>
    public static void Launch()
    {
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
