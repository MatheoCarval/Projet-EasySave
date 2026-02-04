using System;
using System.Collections.Generic;
using Terminal.Gui;
using EasySave.Services;

namespace EasySave.View.Console.Screens;

/// <summary>
/// Settings screen - Handles application settings (language, log format)
/// </summary>
public class SettingsScreen
{
    private readonly LocalizationService _localizationService;
    private FrameView? _frame;
    private Action? _onComplete;

    public SettingsScreen(LocalizationService localizationService)
    {
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
    }

    /// <summary>
    /// Shows settings menu
    /// </summary>
    /// <param name="frame">Content frame to display in</param>
    /// <param name="onComplete">Callback when user returns to menu</param>
    public void Show(FrameView frame, Action onComplete) 
    {
        _frame = frame;
        _onComplete = onComplete;
        
        ShowSettingsMenu();
    }

    /// <summary>
    /// Displays the settings menu with options
    /// </summary>
    private void ShowSettingsMenu()
    {
        _frame!.Title = T("change_settings_title");
        _frame!.RemoveAll();

        // Label
        var label = new Label(T("choose_parameter"))
        {
            X = Pos.Center() - 15,
            Y = Pos.Center() - 3
        };

        // Options list
        var options = new List<string> 
        { 
            T("choose_language"), 
            T("choose_log_format") 
        };
        
        var listView = new ListView(options)
        {
            X = Pos.Center() - 15,
            Y = Pos.Center() - 1,
            Width = 30,
            Height = 4,
            AllowsMarking = false,
            CanFocus = true
        };

        // Select button
        var selectBtn = new Button(T("select"))
        {
            X = Pos.Center() - 10,
            Y = Pos.Center() + 4,
            IsDefault = true
        };

        // Back button
        var backBtn = new Button(T("back"))
        {
            X = Pos.Center() + 8,
            Y = Pos.Center() + 4
        };

        // Button actions
        selectBtn.Clicked += () =>
        {
            switch (listView.SelectedItem)
            {
                case 0:
                    ShowLanguageSelection();
                    break;
                case 1:
                    ShowLogFormatSelection();
                    break;
            }
        };

        backBtn.Clicked += _onComplete;

        _frame!.Add(label, listView, selectBtn, backBtn);
    }

    /// <summary>
    /// Shows language selection screen
    /// </summary>
    private void ShowLanguageSelection()
    {
        _frame!.Title = T("choose_language_title");
        _frame!.RemoveAll();

        // Label
        var label = new Label(T("select_language"))
        {
            X = Pos.Center() - 15,
            Y = Pos.Center() - 3
        };

        // Language options
        var languages = new List<string> { T("language_french"), T("language_english") };
        var languageCodes = new List<string> { "fr", "en" };
        
        var listView = new ListView(languages)
        {
            X = Pos.Center() - 10,
            Y = Pos.Center() - 1,
            Width = 20,
            Height = 4,
            AllowsMarking = false,
            CanFocus = true
        };

        // Select button
        var selectBtn = new Button(T("select"))
        {
            X = Pos.Center() - 10,
            Y = Pos.Center() + 4,
            IsDefault = true
        };

        // Back button
        var backBtn = new Button(T("back"))
        {
            X = Pos.Center() + 8,
            Y = Pos.Center() + 4
        };

        // Button actions
        selectBtn.Clicked += () =>
        {
            var selectedLanguageCode = languageCodes[listView.SelectedItem];
            ChangeLanguage(selectedLanguageCode);
        };

        backBtn.Clicked += ShowSettingsMenu;

        _frame!.Add(label, listView, selectBtn, backBtn);
    }

    /// <summary>
    /// Shows log format selection screen
    /// </summary>
    private void ShowLogFormatSelection()
    {
        _frame!.Title = T("choose_log_format_title");
        _frame!.RemoveAll();

        // Label
        var label = new Label(T("select_format"))
        {
            X = Pos.Center() - 15,
            Y = Pos.Center() - 3
        };

        // Format options
        var formats = new List<string> { "JSON", "XML" };
        var listView = new ListView(formats)
        {
            X = Pos.Center() - 10,
            Y = Pos.Center() - 1,
            Width = 20,
            Height = 3,
            AllowsMarking = false,
            CanFocus = true
        };

        // Select button
        var selectBtn = new Button(T("select"))
        {
            X = Pos.Center() - 10,
            Y = Pos.Center() + 4,
            IsDefault = true
        };

        // Back button
        var backBtn = new Button(T("back"))
        {
            X = Pos.Center() + 8,
            Y = Pos.Center() + 4
        };

        // Button actions
        selectBtn.Clicked += () =>
        {
            var selectedFormat = formats[listView.SelectedItem];
            ChangeLogFormat(selectedFormat);
        };

        backBtn.Clicked += ShowSettingsMenu;

        _frame!.Add(label, listView, selectBtn, backBtn);
    }

    /// <summary>
    /// Changes the application language
    /// </summary>
    /// <param name="languageCode">Language code (fr/en)</param>
    private void ChangeLanguage(string languageCode)
    {
        try
        {
            _localizationService.ChangeLanguage(languageCode);
            MessageBox.Query(50, 7, T("success"), T("language_changed"), T("ok"));
            
            // TODO: Refresh entire UI with new language
            // For now, return to settings menu
            ShowSettingsMenu();
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery(T("error"), ex.Message, T("ok"));
        }
    }

    /// <summary>
    /// Changes the log file format
    /// </summary>
    /// <param name="format">Format (JSON/XML)</param>
    private void ChangeLogFormat(string format)
    {
        try
        {
            // TODO: Implement ConfigurationManager.UpdateLogFormat(format)
            // For now, just show success message
            MessageBox.Query(50, 7, T("success"), T("log_format_changed"), T("ok"));
            ShowSettingsMenu();
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery(T("error"), ex.Message, T("ok"));
        }
    }

    private string T(string key, params object[] args)
    {
        var text = _localizationService.GetTextTranslated(key);
        return args.Length > 0 ? string.Format(text, args) : text;
    }
}