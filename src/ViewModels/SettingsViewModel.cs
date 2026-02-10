using System;
using System.IO;
using System.Windows.Input;
using EasyLog.Enums;
using EasySave.Services;
using EasySave.Services.Managers;
using Avalonia;
using Avalonia.Styling;

namespace EasySave.ViewModels;

/// <summary>
/// ViewModel for the Settings page
/// </summary>
public class SettingsViewModel : ViewModelBase
{
    private readonly ConfigurationManager _configManager;
    private bool _isDarkTheme;
    private int _languageIndex;    // 0 = English, 1 = Français
    private int _logFormatIndex;   // 0 = JSON, 1 = XML
    private string _logFilePath = string.Empty;
    private string _stateFilePath = string.Empty;
    private decimal _maxBackupJobs = 5;
    private bool _hasUnsavedChanges;
    private string _saveMessage = string.Empty;

    public SettingsViewModel()
    {
        _configManager = ConfigurationManager.GetInstance();
        LoadSettings();

        SaveCommand = new RelayCommand(Save);
        ResetCommand = new RelayCommand(LoadSettings);
    }

    #region Properties

    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set
        {
            if (SetProperty(ref _isDarkTheme, value))
            {
                HasUnsavedChanges = true;
                // Apply theme instantly for live preview
                if (Application.Current != null)
                {
                    Application.Current.RequestedThemeVariant = value ? ThemeVariant.Dark : ThemeVariant.Light;
                    View.GUI.App.IsDarkMode = value;
                }
            }
        }
    }

    public int LanguageIndex
    {
        get => _languageIndex;
        set
        {
            if (SetProperty(ref _languageIndex, value))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    public int LogFormatIndex
    {
        get => _logFormatIndex;
        set
        {
            if (SetProperty(ref _logFormatIndex, value))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    public string LogFilePath
    {
        get => _logFilePath;
        set
        {
            if (SetProperty(ref _logFilePath, value))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    public string StateFilePath
    {
        get => _stateFilePath;
        set
        {
            if (SetProperty(ref _stateFilePath, value))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    public decimal MaxBackupJobs
    {
        get => _maxBackupJobs;
        set
        {
            if (value < 1) value = 1;
            if (SetProperty(ref _maxBackupJobs, value))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        set
        {
            SetProperty(ref _hasUnsavedChanges, value);
            if (value) SaveMessage = string.Empty;
        }
    }

    public string SaveMessage
    {
        get => _saveMessage;
        set => SetProperty(ref _saveMessage, value);
    }

    // ── Translated labels ──
    public string TxtSettings => T("gui_settings");
    public string TxtGeneral => T("gui_general");
    public string TxtGeneralDesc => T("gui_general_desc");
    public string TxtDarkMode => T("gui_dark_mode");
    public string TxtDarkModeDesc => T("gui_dark_mode_desc");
    public string TxtLanguage => T("gui_language");
    public string TxtLanguageDesc => T("gui_language_desc");
    public string TxtLogging => T("gui_logging");
    public string TxtLoggingDesc => T("gui_logging_desc");
    public string TxtLogFormat => T("gui_log_format");
    public string TxtLogFormatDesc => T("gui_log_format_desc");
    public string TxtLogPath => T("gui_log_path");
    public string TxtLogPathDesc => T("gui_log_path_desc");
    public string TxtState => T("gui_state");
    public string TxtStateDesc => T("gui_state_desc");
    public string TxtStatePath => T("gui_state_path");
    public string TxtStatePathDesc => T("gui_state_path_desc");
    public string TxtAbout => T("gui_about");
    public string TxtAboutDesc => T("gui_about_desc");
    public string TxtVersion => T("gui_version");
    public string TxtRuntime => T("gui_runtime");
    public string TxtUiFramework => T("gui_ui_framework");
    public string TxtOs => T("gui_os");
    public string TxtUnsaved => T("gui_unsaved");
    public string TxtReset => T("gui_reset");
    public string TxtSave => T("gui_save");
    public string TxtBrowse => T("gui_browse");
    public string TxtOn => T("gui_on");
    public string TxtOff => T("gui_off");
    public string TxtBackup => T("gui_backup");
    public string TxtBackupDesc => T("gui_backup_desc");
    public string TxtMaxJobs => T("gui_max_jobs");
    public string TxtMaxJobsDesc => T("gui_max_jobs_desc");

    public string AppVersion => "2.0";
    public string DotNetVersion => $".NET {Environment.Version}";
    public string AvaloniaVersion => "11.0.10";
    public string OSInfo => $"{Environment.OSVersion.Platform} {Environment.OSVersion.Version}";
    public string Architecture => System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString();

    #endregion

    #region Commands

    public ICommand SaveCommand { get; }
    public ICommand ResetCommand { get; }

    #endregion

    #region Methods

    public void LoadSettings()
    {
        var config = _configManager.LoadConfiguration();

        // Theme
        _isDarkTheme = View.GUI.App.IsDarkMode;
        OnPropertyChanged(nameof(IsDarkTheme));

        // Language — config stores "fr-FR" or "en-US", map to index
        var lang = config.GetLanguage();
        _languageIndex = lang.StartsWith("fr", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        OnPropertyChanged(nameof(LanguageIndex));

        // Log format
        _logFormatIndex = config.GetLogFormat() == LogFormat.XML ? 1 : 0;
        OnPropertyChanged(nameof(LogFormatIndex));

        // Max backup jobs
        _maxBackupJobs = config.GetMaxBackupJobs();
        OnPropertyChanged(nameof(MaxBackupJobs));

        // Paths
        _logFilePath = config.GetLogFilePath();
        if (string.IsNullOrEmpty(_logFilePath))
            _logFilePath = config.GetDefaultLogPath();
        OnPropertyChanged(nameof(LogFilePath));

        _stateFilePath = config.GetStateFilePath();
        if (string.IsNullOrEmpty(_stateFilePath))
            _stateFilePath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EasySave", "state.json");
        OnPropertyChanged(nameof(StateFilePath));

        HasUnsavedChanges = false;
        SaveMessage = string.Empty;
    }

    private void Save()
    {
        try
        {
            // Language — use short code for LocalizationService, long code for config
            string shortLang = _languageIndex == 1 ? "fr" : "en";
            string langCode = _languageIndex == 1 ? "fr-FR" : "en-US";

            // Apply language live
            View.GUI.App.LocalizationService?.ChangeLanguage(shortLang);

            // Log format
            var format = _logFormatIndex == 1 ? LogFormat.XML : LogFormat.JSON;

            // Paths — ensure they point to files, not directories
            string logPath = _logFilePath;
            if (!string.IsNullOrWhiteSpace(logPath) && Directory.Exists(logPath))
            {
                string ext = format == LogFormat.XML ? "xml" : "json";
                logPath = System.IO.Path.Combine(logPath, $"jobs.{ext}");
            }

            string statePath = _stateFilePath;
            if (!string.IsNullOrWhiteSpace(statePath) && Directory.Exists(statePath))
            {
                statePath = System.IO.Path.Combine(statePath, "state.json");
            }

            // Single load → update all fields → single save (atomic write to Config.json)
            var config = _configManager.LoadConfiguration();
            config.SetLanguage(langCode);
            config.SetLogFormat(format);
            config.SetMaxBackupJobs((int)_maxBackupJobs);
            config.SetDarkMode(_isDarkTheme);
            if (!string.IsNullOrWhiteSpace(logPath))
                config.SetLogFilePath(logPath);
            if (!string.IsNullOrWhiteSpace(statePath))
                config.SetStateFilePath(statePath);
            _configManager.SaveConfiguration(config);

            // Apply theme live
            if (Application.Current != null)
            {
                Application.Current.RequestedThemeVariant = _isDarkTheme ? ThemeVariant.Dark : ThemeVariant.Light;
                View.GUI.App.IsDarkMode = _isDarkTheme;
            }

            // Refresh all translated labels
            RefreshTranslations();

            HasUnsavedChanges = false;
            SaveMessage = T("gui_saved_ok");
        }
        catch (Exception ex)
        {
            SaveMessage = $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Helper to get translated text from the localization service
    /// </summary>
    private string T(string key)
    {
        try
        {
            return View.GUI.App.LocalizationService?.GetTextTranslated(key) ?? key;
        }
        catch
        {
            return key;
        }
    }

    /// <summary>
    /// Notify all translated properties have changed so UI refreshes
    /// </summary>
    private void RefreshTranslations()
    {
        OnPropertyChanged(nameof(TxtSettings));
        OnPropertyChanged(nameof(TxtGeneral));
        OnPropertyChanged(nameof(TxtGeneralDesc));
        OnPropertyChanged(nameof(TxtDarkMode));
        OnPropertyChanged(nameof(TxtDarkModeDesc));
        OnPropertyChanged(nameof(TxtLanguage));
        OnPropertyChanged(nameof(TxtLanguageDesc));
        OnPropertyChanged(nameof(TxtLogging));
        OnPropertyChanged(nameof(TxtLoggingDesc));
        OnPropertyChanged(nameof(TxtLogFormat));
        OnPropertyChanged(nameof(TxtLogFormatDesc));
        OnPropertyChanged(nameof(TxtLogPath));
        OnPropertyChanged(nameof(TxtLogPathDesc));
        OnPropertyChanged(nameof(TxtState));
        OnPropertyChanged(nameof(TxtStateDesc));
        OnPropertyChanged(nameof(TxtStatePath));
        OnPropertyChanged(nameof(TxtStatePathDesc));
        OnPropertyChanged(nameof(TxtAbout));
        OnPropertyChanged(nameof(TxtAboutDesc));
        OnPropertyChanged(nameof(TxtVersion));
        OnPropertyChanged(nameof(TxtRuntime));
        OnPropertyChanged(nameof(TxtUiFramework));
        OnPropertyChanged(nameof(TxtOs));
        OnPropertyChanged(nameof(TxtUnsaved));
        OnPropertyChanged(nameof(TxtReset));
        OnPropertyChanged(nameof(TxtSave));
        OnPropertyChanged(nameof(TxtBrowse));
        OnPropertyChanged(nameof(TxtOn));
        OnPropertyChanged(nameof(TxtOff));
        OnPropertyChanged(nameof(TxtBackup));
        OnPropertyChanged(nameof(TxtBackupDesc));
        OnPropertyChanged(nameof(TxtMaxJobs));
        OnPropertyChanged(nameof(TxtMaxJobsDesc));
    }

    #endregion
}
