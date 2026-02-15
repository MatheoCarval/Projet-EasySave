using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
    /// <summary>
    /// Raised after settings are saved so other components can react immediately.
    /// </summary>
    public event EventHandler? SettingsSaved;
    private readonly ConfigurationManager _configManager;
    private bool _isDarkTheme;
    private int _languageIndex;    // 0 = English, 1 = Français
    private int _logFormatIndex;   // 0 = JSON, 1 = XML
    private string _logFilePath = string.Empty;
    private string _stateFilePath = string.Empty;
    private string _cryptosoftPath = string.Empty;
    private string _cryptosoftPublicKey = string.Empty;
    private string _encryptedExtensionsText = string.Empty;
    private string _blockedApplicationsText = string.Empty;
    private string? _selectedDetectedApplication;
    private string _detectedAppsSearch = string.Empty;
    private bool _hasUnsavedChanges;
    private string _saveMessage = string.Empty;

    public SettingsViewModel()
    {
        _configManager = ConfigurationManager.GetInstance();
        DetectedApplications = new ObservableCollection<string>();
        FilteredDetectedApplications = new ObservableCollection<string>();
        LoadSettings();

        SaveCommand = new RelayCommand(Save);
        ResetCommand = new RelayCommand(LoadSettings);
        RefreshDetectedAppsCommand = new RelayCommand(RefreshDetectedApplications);
        AddDetectedAppCommand = new RelayCommand(AddSelectedDetectedApplication, () => !string.IsNullOrWhiteSpace(SelectedDetectedApplication));
        ClearDetectedAppsSearchCommand = new RelayCommand(() => DetectedAppsSearch = string.Empty);
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

    public string CryptosoftPath
    {
        get => _cryptosoftPath;
        set
        {
            if (SetProperty(ref _cryptosoftPath, value))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    public string CryptosoftPublicKey
    {
        get => _cryptosoftPublicKey;
        set
        {
            if (SetProperty(ref _cryptosoftPublicKey, value))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    public string EncryptedExtensionsText
    {
        get => _encryptedExtensionsText;
        set
        {
            if (SetProperty(ref _encryptedExtensionsText, value))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    public string BlockedApplicationsText
    {
        get => _blockedApplicationsText;
        set
        {
            if (SetProperty(ref _blockedApplicationsText, value))
            {
                HasUnsavedChanges = true;
            }
        }
    }

    public ObservableCollection<string> DetectedApplications { get; }
    public ObservableCollection<string> FilteredDetectedApplications { get; }

    public string DetectedAppsSearch
    {
        get => _detectedAppsSearch;
        set
        {
            if (SetProperty(ref _detectedAppsSearch, value))
            {
                ApplyDetectedAppsFilter();
            }
        }
    }

    public string? SelectedDetectedApplication
    {
        get => _selectedDetectedApplication;
        set
        {
            if (SetProperty(ref _selectedDetectedApplication, value))
            {
                ((RelayCommand)AddDetectedAppCommand).RaiseCanExecuteChanged();
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
    public string TxtBackup => T("gui_backup");
    public string TxtBackupDesc => T("gui_backup_desc");
    public string TxtEncryption => T("gui_encryption");
    public string TxtEncryptionDesc => T("gui_encryption_desc");
    public string TxtCryptosoftPath => T("gui_cryptosoft_path");
    public string TxtCryptosoftPathDesc => T("gui_cryptosoft_path_desc");
    public string TxtCryptosoftPublicKey => T("gui_cryptosoft_public_key");
    public string TxtCryptosoftPublicKeyDesc => T("gui_cryptosoft_public_key_desc");
    public string TxtEncryptedExtensions => T("gui_encrypted_extensions");
    public string TxtEncryptedExtensionsDesc => T("gui_encrypted_extensions_desc");
    public string TxtEncryptedExtensionsPlaceholder => T("gui_encrypted_extensions_placeholder");
    public string TxtModalEncryptedExtensions => T("gui_modal_encrypted_extensions");
    public string TxtModalEncryptedExtensionsDesc => T("gui_modal_encrypted_extensions_desc");
    public string TxtModalEncryptedExtensionsPlaceholder => T("gui_modal_encrypted_extensions_placeholder");
    public string TxtBlockedApps => T("gui_blocked_apps");
    public string TxtBlockedAppsDesc => T("gui_blocked_apps_desc");
    public string TxtBlockedAppsPlaceholder => T("gui_blocked_apps_placeholder");
    public string TxtDetectedApps => T("gui_detected_apps");
    public string TxtDetectedAppsDesc => T("gui_detected_apps_desc");
    public string TxtDetectAppsButton => T("gui_detect_apps_button");
    public string TxtAddBlockedAppButton => T("gui_add_blocked_app_button");
    public string TxtBrowseExeButton => T("gui_browse_exe_button");
    public string TxtSearchProcessPlaceholder => T("gui_search_process_placeholder");
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
    public string AppVersion => "2.0";
    public string DotNetVersion => $".NET {Environment.Version}";
    public string AvaloniaVersion => "11.0.10";
    public string OSInfo => $"{Environment.OSVersion.Platform} {Environment.OSVersion.Version}";
    public string Architecture => System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString();

    #endregion

    #region Commands

    public ICommand SaveCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand RefreshDetectedAppsCommand { get; }
    public ICommand AddDetectedAppCommand { get; }
    public ICommand ClearDetectedAppsSearchCommand { get; }

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

        _cryptosoftPath = config.GetCryptosoftPath();
        OnPropertyChanged(nameof(CryptosoftPath));

        _cryptosoftPublicKey = config.GetCryptosoftPublicKey();
        OnPropertyChanged(nameof(CryptosoftPublicKey));

        _encryptedExtensionsText = string.Join(Environment.NewLine, config.GetEncryptedExtensions());
        OnPropertyChanged(nameof(EncryptedExtensionsText));

        var blockedApps = config.GetBlockedApplications();
        _blockedApplicationsText = string.Join(Environment.NewLine, blockedApps);
        OnPropertyChanged(nameof(BlockedApplicationsText));

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

            // Paths — log path can be a directory (for daily logs) or a file
            string logPath = _logFilePath;

            string statePath = _stateFilePath;
            if (!string.IsNullOrWhiteSpace(statePath) && Directory.Exists(statePath))
            {
                statePath = System.IO.Path.Combine(statePath, "state.json");
            }

            // Single load → update all fields → single save (atomic write to Config.json)
            var config = _configManager.LoadConfiguration();
            config.SetLanguage(langCode);
            config.SetLogFormat(format);
            config.SetDarkMode(_isDarkTheme);
            config.SetBlockedApplications(ParseBlockedApplications(_blockedApplicationsText));
            config.SetCryptosoftPath(_cryptosoftPath);
            config.SetCryptosoftPublicKey(_cryptosoftPublicKey);
            config.SetEncryptedExtensions(ParseEncryptedExtensions(_encryptedExtensionsText));
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

            // Notify subscribers (e.g. MainViewModel) so changes apply immediately
            SettingsSaved?.Invoke(this, EventArgs.Empty);
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
        OnPropertyChanged(nameof(TxtBackup));
        OnPropertyChanged(nameof(TxtBackupDesc));
        OnPropertyChanged(nameof(TxtEncryption));
        OnPropertyChanged(nameof(TxtEncryptionDesc));
        OnPropertyChanged(nameof(TxtCryptosoftPath));
        OnPropertyChanged(nameof(TxtCryptosoftPathDesc));
        OnPropertyChanged(nameof(TxtEncryptedExtensions));
        OnPropertyChanged(nameof(TxtEncryptedExtensionsDesc));
        OnPropertyChanged(nameof(TxtEncryptedExtensionsPlaceholder));
        OnPropertyChanged(nameof(TxtModalEncryptedExtensions));
        OnPropertyChanged(nameof(TxtModalEncryptedExtensionsDesc));
        OnPropertyChanged(nameof(TxtModalEncryptedExtensionsPlaceholder));
        OnPropertyChanged(nameof(TxtBlockedApps));
        OnPropertyChanged(nameof(TxtBlockedAppsDesc));
        OnPropertyChanged(nameof(TxtBlockedAppsPlaceholder));
        OnPropertyChanged(nameof(TxtDetectedApps));
        OnPropertyChanged(nameof(TxtDetectedAppsDesc));
        OnPropertyChanged(nameof(TxtDetectAppsButton));
        OnPropertyChanged(nameof(TxtAddBlockedAppButton));
        OnPropertyChanged(nameof(TxtBrowseExeButton));
        OnPropertyChanged(nameof(TxtSearchProcessPlaceholder));
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
    }

    private static List<string> ParseBlockedApplications(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new List<string>();
        }

        var separators = new[] { ',', ';', '\n', '\r' };
        return text
            .Split(separators, StringSplitOptions.RemoveEmptyEntries)
            .Select(app => app.Trim())
            .Where(app => !string.IsNullOrWhiteSpace(app))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<string> ParseEncryptedExtensions(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new List<string>();
        }

        var separators = new[] { ',', ';', '\n', '\r' };
        return text
            .Split(separators, StringSplitOptions.RemoveEmptyEntries)
            .Select(ext => ext.Trim())
            .Where(ext => !string.IsNullOrWhiteSpace(ext))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private void RefreshDetectedApplications()
    {
        var detected = DetectRunningApplications();
        DetectedApplications.Clear();
        foreach (var app in detected.OrderBy(a => a, StringComparer.OrdinalIgnoreCase))
        {
            DetectedApplications.Add(app);
        }
        ApplyDetectedAppsFilter();
    }

    private void ApplyDetectedAppsFilter()
    {
        FilteredDetectedApplications.Clear();
        var query = _detectedAppsSearch?.Trim() ?? string.Empty;
        var filtered = string.IsNullOrEmpty(query)
            ? DetectedApplications
            : DetectedApplications.Where(a => a.Contains(query, StringComparison.OrdinalIgnoreCase));
        foreach (var app in filtered)
        {
            FilteredDetectedApplications.Add(app);
        }
    }

    private void AddSelectedDetectedApplication()
    {
        if (string.IsNullOrWhiteSpace(SelectedDetectedApplication))
        {
            return;
        }

        var items = ParseBlockedApplications(_blockedApplicationsText);
        if (!items.Contains(SelectedDetectedApplication, StringComparer.OrdinalIgnoreCase))
        {
            items.Add(SelectedDetectedApplication);
            BlockedApplicationsText = string.Join(Environment.NewLine, items);
        }
    }

    /// <summary>
    /// Adds a process name to the blocked applications list (called from file picker).
    /// </summary>
    public void AddBlockedApplication(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName)) return;

        var items = ParseBlockedApplications(_blockedApplicationsText);
        if (!items.Contains(processName, StringComparer.OrdinalIgnoreCase))
        {
            items.Add(processName);
            BlockedApplicationsText = string.Join(Environment.NewLine, items);
        }
    }

    /// <summary>
    /// Detects currently running processes on the system.
    /// Cross-platform: uses System.Diagnostics.Process which works on Windows, macOS and Linux.
    /// Filters out common system/background processes to show only user-relevant applications.
    /// </summary>
    private static List<string> DetectRunningApplications()
    {
        // Common system/background processes to hide (cross-platform)
        var systemProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Windows system processes
            "svchost", "csrss", "wininit", "winlogon", "lsass", "services", "smss",
            "system", "idle", "registry", "dwm", "fontdrvhost", "sihost",
            "taskhostw", "ctfmon", "conhost", "dllhost", "wudfhost",
            "runtimebroker", "searchhost", "startmenuexperiencehost",
            "shellexperiencehost", "textinputhost", "widgetservice",
            "securityhealthservice", "securityhealthsystray",
            "spoolsv", "lsaiso", "memcompression", "ntoskrnl",
            "audiodg", "dashost", "unsecapp", "wmiprvse",
            "searchindexer", "searchprotocolhost", "searchfilterhost",
            "sgrmbroker", "msdtc", "sppsvc", "sedsvc",
            "systemsettingsbroker", "backgroundtaskhost", "backgroundtransferhost",
            "applicationframehost", "lockapp", "comppkgsrv",
            // macOS system processes
            "launchd", "kernel_task", "loginwindow", "windowserver",
            "opendirectoryd", "diskarbitrationd", "coreservicesd",
            "airportd", "bluetoothd", "configd", "mds", "mds_stores",
            "notifyd", "powerd", "syslogd", "thermald",
            // Linux system processes
            "systemd", "kthreadd", "ksoftirqd", "kworker", "rcu_gp",
            "rcu_sched", "migration", "cpuhp", "init", "dbus-daemon",
            "polkitd", "udisksd", "networkmanager", "pipewire",
            "wireplumber", "xdg-desktop-portal"
        };

        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var process in Process.GetProcesses())
        {
            try
            {
                var name = process.ProcessName;
                if (!string.IsNullOrWhiteSpace(name) && !systemProcesses.Contains(name))
                {
                    result.Add(name);
                }
            }
            catch
            {
                // Ignore processes that cannot be accessed.
            }
            finally
            {
                process.Dispose();
            }
        }

        return result.ToList();
    }

    #endregion
}
