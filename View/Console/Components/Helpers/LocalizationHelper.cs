using System;
using EasySave.Services;
using Models.Enums;

namespace EasySave.View.Console.Helpers;

/// <summary>
/// Localization Helper - Simplifies access to translations
/// Provides shortcuts and formatting for common translation patterns
/// </summary>
public class LocalizationHelper
{
    private readonly LocalizationService _localizationService;

    public LocalizationHelper(LocalizationService localizationService)
    {
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
    }

    /// <summary>
    /// Gets a translated string
    /// </summary>
    public string T(string key)
    {
        return _localizationService.GetTextTranslated(key);
    }

    /// <summary>
    /// Gets a translated string with formatting
    /// </summary>
    public string T(string key, params object[] args)
    {
        var text = _localizationService.GetTextTranslated(key);
        return args.Length > 0 ? string.Format(text, args) : text;
    }

    /// <summary>
    /// Gets display text for backup type
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
    /// Gets display text for backup state
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
    /// Gets backup type from index (0 = COMPLETE, 1 = DIFFERENTIAL)
    /// </summary>
    public BackupType GetBackupTypeFromIndex(int index)
    {
        return index == 0 ? BackupType.COMPLETE : BackupType.DIFFERENTIAL;
    }

    /// <summary>
    /// Gets index from backup type (COMPLETE = 0, DIFFERENTIAL = 1)
    /// </summary>
    public int GetIndexFromBackupType(BackupType type)
    {
        return type == BackupType.COMPLETE ? 0 : 1;
    }

    /// <summary>
    /// Formats a list of items with numbers
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
    /// Formats progress string
    /// </summary>
    public string FormatProgress(int progress, int current, int total)
    {
        return T("progress_format", progress, current, total);
    }
}