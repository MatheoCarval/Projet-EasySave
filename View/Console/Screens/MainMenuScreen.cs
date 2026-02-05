using System;
using System.Collections.Generic;
using Terminal.Gui;
using EasySave.Services;

namespace EasySave.View.Console.Screens;

/// <summary>
/// Displays the main menu screen with eight primary navigation options for the user interface.
/// </summary>
public class MainMenuScreen
{
    /// <summary>
    /// Service for retrieving localized text strings for menu items and labels.
    /// </summary>
    private readonly LocalizationService _localizationService;

    /// <summary>
    /// Initializes a new instance of MainMenuScreen with the specified localization service for menu item translation.
    /// </summary>
    public MainMenuScreen(LocalizationService localizationService)
    {
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
    }

    /// <summary>
    /// Creates and returns a centered ListView containing all eight main menu options, with a callback handler for selection events.
    /// </summary>
    public Terminal.Gui.View GetView(Action<int> onSelection)
    {
        var items = CreateMenuItems();

        var listView = new ListView(items)
        {
            X = Pos.Center() - 15,
            Y = Pos.Center() - 4,
            Width = 30,
            Height = 8,
            AllowsMarking = false,
            CanFocus = true
        };

        listView.OpenSelectedItem += (e) => onSelection(listView.SelectedItem);

        return listView;
    }

    /// <summary>
    /// Creates the list of localized menu item strings representing the eight main navigation options.
    /// </summary>
    private List<string> CreateMenuItems()
    {
        return new List<string>
        {
            T("menu_create_task"),
            T("menu_modify_task"),
            T("menu_delete_task"),
            T("menu_execute_task"),
            T("menu_execute_all_tasks"),
            T("menu_display_tasks"),
            T("menu_change_settings"),
            T("menu_quit_main")
        };
    }

    /// <summary>
    /// Retrieves and returns the localized text for the specified translation key.
    /// </summary>
    private string T(string key) => _localizationService.GetTextTranslated(key);
}