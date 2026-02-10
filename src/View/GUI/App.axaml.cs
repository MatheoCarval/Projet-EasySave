using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using EasySave.Services;
using Services.Managers;

namespace EasySave.View.GUI;

/// <summary>
/// Avalonia application entry point
/// </summary>
public class App : Application
{
    /// <summary>
    /// Shared localization service instance
    /// </summary>
    public static LocalizationService? LocalizationService { get; set; }

    /// <summary>
    /// Shared backup manager instance
    /// </summary>
    public static BackupManager? BackupManager { get; set; }

    /// <summary>
    /// Controls whether the app uses dark mode (true) or light mode (false).
    /// Must be set before calling Launch().
    /// </summary>
    public static bool IsDarkMode { get; set; } = false;

    /// <summary>
    /// Initializes the application by loading XAML and applying the theme
    /// </summary>
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        RequestedThemeVariant = IsDarkMode ? ThemeVariant.Dark : ThemeVariant.Light;
    }

    /// <summary>
    /// Called when the application is ready to start
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
