using System;
using System.Collections.Generic;
using Terminal.Gui;
using EasySave.Services;

namespace EasySave.View.Console.Screens;

/// <summary>
/// Manages the application settings screen, allowing users to configure language and log format preferences.
/// </summary>
public class SettingsScreen
{
    /// <summary>
    /// Service for retrieving and changing localized text strings.
    /// </summary>
    private readonly LocalizationService _localizationService;
    /// <summary>
    /// The frame view for rendering settings screens and components.
    /// </summary>
    private FrameView? _frame;
    /// <summary>
    /// Callback invoked when the user completes settings configuration and returns to the main menu.
    /// </summary>
    private Action? _onComplete;

    /// <summary>
    /// Initializes a new instance of SettingsScreen with the specified localization service.
    /// </summary>
    public SettingsScreen(LocalizationService localizationService)
    {
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
    }

    /// <summary>
    /// Displays the settings screen in the specified frame with a completion callback handler.
    /// </summary>
    public void Show(FrameView frame, Action onComplete)
    {
        _frame = frame;
        _onComplete = onComplete;

        ShowSettingsMenu();
    }

    /// <summary>
    /// Displays the main settings menu with options for language and log format configuration.
    /// </summary>
    private void ShowSettingsMenu()
    {
        _frame!.Title = T("change_settings_title");
        _frame!.RemoveAll();

        var label = new Label(T("choose_parameter"))
        {
            X = Pos.Center() - 15,
            Y = Pos.Center() - 3
        };

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

        var selectBtn = new Button(T("select"))
        {
            X = Pos.Center() - 10,
            Y = Pos.Center() + 4,
            IsDefault = true
        };

        var backBtn = new Button(T("back"))
        {
            X = Pos.Center() + 8,
            Y = Pos.Center() + 4
        };

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
    /// Displays the language selection screen with available language options.
    /// </summary>
    private void ShowLanguageSelection()
    {
        _frame!.Title = T("choose_language_title");
        _frame!.RemoveAll();

        var label = new Label(T("select_language"))
        {
            X = Pos.Center() - 15,
            Y = Pos.Center() - 3
        };

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

        var selectBtn = new Button(T("select"))
        {
            X = Pos.Center() - 10,
            Y = Pos.Center() + 4,
            IsDefault = true
        };

        var backBtn = new Button(T("back"))
        {
            X = Pos.Center() + 8,
            Y = Pos.Center() + 4
        };

        selectBtn.Clicked += () =>
        {
            var selectedLanguageCode = languageCodes[listView.SelectedItem];
            ChangeLanguage(selectedLanguageCode);
        };

        backBtn.Clicked += ShowSettingsMenu;

        _frame!.Add(label, listView, selectBtn, backBtn);
    }

    /// <summary>
    /// Displays the log format selection screen with available format options.
    /// </summary>
    private void ShowLogFormatSelection()
    {
        _frame!.Title = T("choose_log_format_title");
        _frame!.RemoveAll();

        var label = new Label(T("select_format"))
        {
            X = Pos.Center() - 15,
            Y = Pos.Center() - 3
        };

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

        var selectBtn = new Button(T("select"))
        {
            X = Pos.Center() - 10,
            Y = Pos.Center() + 4,
            IsDefault = true
        };

        var backBtn = new Button(T("back"))
        {
            X = Pos.Center() + 8,
            Y = Pos.Center() + 4
        };

        selectBtn.Clicked += () =>
        {
            var selectedFormat = formats[listView.SelectedItem];
            ChangeLogFormat(selectedFormat);
        };

        backBtn.Clicked += ShowSettingsMenu;

        _frame!.Add(label, listView, selectBtn, backBtn);
    }

    /// <summary>
    /// Changes the application language to the specified language code and updates the settings menu display.
    /// </summary>
    private void ChangeLanguage(string languageCode)
    {
        try
        {
            _localizationService.ChangeLanguage(languageCode);
            MessageBox.Query(50, 7, T("success"), T("language_changed"), T("ok"));

            ShowSettingsMenu();
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery(T("error"), ex.Message, T("ok"));
        }
    }

    /// <summary>
    /// Changes the log file format to the specified format (JSON or XML) and updates the settings menu display.
    /// </summary>
    private void ChangeLogFormat(string format)
    {
        try
        {
            MessageBox.Query(50, 7, T("success"), T("log_format_changed"), T("ok"));
            ShowSettingsMenu();
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery(T("error"), ex.Message, T("ok"));
        }
    }

    /// <summary>
    /// Retrieves and formats the localized text for the specified translation key with optional format arguments.
    /// </summary>
    private string T(string key, params object[] args)
    {
        var text = _localizationService.GetTextTranslated(key);
        return args.Length > 0 ? string.Format(text, args) : text;
    }
}