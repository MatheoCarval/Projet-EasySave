using System;
using System.Collections.Generic;
using Terminal.Gui;
using EasySave.Services;

namespace EasySave.View.Console.Screens;

/// <summary>
/// Main menu screen - Displays the 8 main menu options
/// </summary>
public class MainMenuScreen
{
    private readonly LocalizationService _localizationService;

    public MainMenuScreen(LocalizationService localizationService)
    {
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
    }

    /// <summary>
    /// Gets the menu view with all options
    /// </summary>
    /// <param name="onSelection">Callback when user selects an option</param>
    /// <returns>ListView containing menu items</returns>
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
    /// Creates the list of menu items with translations
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

    private string T(string key) => _localizationService.GetTextTranslated(key);
}