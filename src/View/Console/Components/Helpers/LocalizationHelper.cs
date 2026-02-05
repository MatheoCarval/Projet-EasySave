using System;
using EasySave.Services;
using Models.Enums;

namespace EasySave.View.Console.Helpers;

/// <summary>
/// Helper utility class providing simplified access to localization services with convenience methods for translation retrieval, formatting, and type-specific display text generation.
/// </summary>
public class LocalizationHelper
{
    /// <summary>
    /// Service for retrieving localized text strings based on the current language setting.
    /// </summary>
    private readonly LocalizationService _localizationService;

    /// <summary>
    /// Initializes a new instance of the LocalizationHelper with a localization service for retrieving translated text.
    /// </summary>
    public LocalizationHelper(LocalizationService localizationService)
    {
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
    }

    /// <summary>
    /// Retrieves the localized text string for the specified translation key.
    /// </summary>
    public string T(string key)
    {
        return _localizationService.GetTextTranslated(key);
    }

    /// <summary>
    /// Retrieves the localized text string for the specified translation key and applies string formatting with the provided arguments.
    /// </summary>
    public string T(string key, params object[] args)
    {
        var text = _localizationService.GetTextTranslated(key);
        return args.Length > 0 ? string.Format(text, args) : text;
    }

    /// <summary>
    /// Retrieves the localized display text for the specified backup type enumeration value.
    /// </summary>
    public string GetBackupTypeDisplay(BackupType type)
    {
        return type switch
        {
            BackupType.COMPLETE => T("backup_type_full"),
            BackupType.DIFFERENTIAL => T("backup_type_differential"),
            _ => type.ToString()
        };
    }

    /// <summary>
    /// Retrieves the localized display text for the specified backup state enumeration value.
    /// </summary>
    public string GetStateDisplay(BackupState state)
    {
        return state switch
        {
            BackupState.PENDING => T("state_not_started"),
            BackupState.ACTIVE => T("state_active"),
            BackupState.COMPLETED => T("state_completed"),
            BackupState.ERROR => T("state_error"),
            _ => state.ToString()
        };
    }

    /// <summary>
    /// Converts a zero-based index to the corresponding backup type enumeration value (0 = COMPLETE, 1 = DIFFERENTIAL).
    /// </summary>
    public BackupType GetBackupTypeFromIndex(int index)
    {
        return index == 0 ? BackupType.COMPLETE : BackupType.DIFFERENTIAL;
    }

    /// <summary>
    /// Converts a backup type enumeration value to its corresponding zero-based index representation (COMPLETE = 0, DIFFERENTIAL = 1).
    /// </summary>
    public int GetIndexFromBackupType(BackupType type)
    {
        return type == BackupType.COMPLETE ? 0 : 1;
    }

    /// <summary>
    /// Formats a list of strings into a numbered display format with optional custom separator, where each item is prefixed with its 1-based index in brackets.
    /// </summary>
    public string FormatList(System.Collections.Generic.List<string> items, string separator = "\n")
    {
        var formatted = new System.Collections.Generic.List<string>();
        for (int i = 0; i < items.Count; i++)
        {
            formatted.Add($"  [{i + 1}] {items[i]}");
        }
        return string.Join(separator, formatted);
    }

    /// <summary>
    /// Formats a progress display string using the localized progress format template and the specified progress percentage, current count, and total count values.
    /// </summary>
    public string FormatProgress(int progress, int current, int total)
    {
        return T("progress_format", progress, current, total);
    }
}